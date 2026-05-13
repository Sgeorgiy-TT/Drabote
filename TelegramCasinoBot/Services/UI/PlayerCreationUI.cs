using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Data.Creation;
using TelegramCasinoBot.Services.Models.Data.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI.Steps;
using TelegramCasinoBot.Services.UI.Steps.Dispatcher;
using TelegramCasinoBot.Services.UI.Steps.StepsCreation;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.UI
{
    public interface IStepBasedCreationUI
    {
        bool IsInCreation(long chatId);
        void StartCreation(long chatId);
        Task HandleInput(long chatId, string data);
        Task HandleCallback(long chatId, CallbackQuery callbackQuery);
        Task RestartCreation(long chatId);
    }

    public class PlayerCreationUI : IStepBasedCreationUI
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<PlayerCreationUI> _logger;
        private readonly List<ICreationStep> _steps;
        private readonly DatabaseService _databaseService;
        private readonly PlayerManager _playerManager;
        private readonly AbilityService _abilityService;
        private readonly ImageService _imageService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly LocationService _locationService;
        private readonly Dictionary<long, int> _currentStepIndex = new();
        private readonly Dictionary<long, Player.PlayerBuilder> _playerbuilder = new();
        private readonly Dictionary<long, Stack<int>> _stepHistory = new();
        public IReadOnlyList<ICreationStep> Steps => _steps;

        public PlayerCreationUI(
            TelegramBotClient botClient,
            ILogger<PlayerCreationUI> logger,
            IRaceService raceService,
            IClassService classService,
            CharacterIconService characterIconService,
            DatabaseService databaseService,
            PlayerManager playerManager,
            AbilityService abilityService,
            ImageService imageService,
            LocationService locationService,
            ILoggerFactory loggerFactory)
        {
            _botClient = botClient;
            _databaseService = databaseService;
            _playerManager = playerManager;
            _logger = logger;
            _abilityService = abilityService;
            _imageService = imageService;
            _locationService = locationService;
            _loggerFactory = loggerFactory;

            _steps = new List<ICreationStep>
            {
                new NameStep(botClient, async chatId => await AdvanceStep(chatId), async chatId => await RestartCreation(chatId)),
                new GenderStep(botClient, async chatId => await AdvanceStep(chatId), async chatId => await RestartCreation(chatId), async chatId => await GoBack(chatId), _loggerFactory.CreateLogger<GenderStep>()),
                new RaceStep(botClient, raceService, async chatId => await AdvanceStep(chatId), async chatId => await RestartCreation(chatId), async chatId => await GoBack(chatId), _loggerFactory.CreateLogger<RaceStep>()),
                new ClassStep(botClient, classService, async chatId => await AdvanceStep(chatId), async chatId => await RestartCreation(chatId), async chatId => await GoBack(chatId), _loggerFactory.CreateLogger<ClassStep>()),
                new IconStep(botClient, characterIconService, async chatId => await AdvanceStep(chatId), async chatId => await RestartCreation(chatId), async chatId => await GoBack(chatId), _loggerFactory.CreateLogger<IconStep>()),
                new SummaryStep(botClient, async chatId => await AdvanceStep(chatId), async chatId => await RestartCreation(chatId), _loggerFactory.CreateLogger<SummaryStep>())
            };

        }

        public bool IsInCreation(long chatId) => _playerbuilder.ContainsKey(chatId);
        bool IStepBasedCreationUI.IsInCreation(long chatId) => IsInCreation(chatId);

        public void StartCreation(long chatId)
        {
            _logger.LogDebug("[{ChatId}] Начало создания персонажа", chatId);
            var builder = new Player.PlayerBuilder(_imageService, _abilityService);
            builder.SetChatId(chatId);
            _playerbuilder[chatId] = builder;
            var stack = new Stack<int>();
            stack.Push(0);
            _stepHistory[chatId] = stack;
            _logger.LogDebug("[{ChatId}] Создан билдер: Hash={Hash}", chatId, builder.GetHashCode());
            _ = _steps[0].Ask(chatId, builder);
        }
        public async Task AdvanceStep(long chatId)
        {
            if (!_playerbuilder.TryGetValue(chatId, out var builder))
            {
                _logger.LogWarning("[{ChatId}] Билдер не найден при AdvanceStep", chatId);
                return;
            }
            if (!_stepHistory.TryGetValue(chatId, out var stack) || stack.Count == 0)
            {
                _logger.LogWarning("[{ChatId}] Стек истории пуст", chatId);
                return;
            }
            int currentIndex = stack.Peek();
            int nextIndex = currentIndex + 1;
            if (nextIndex >= _steps.Count)
            {
                await CompleteCreation(chatId);
                return;
            }
            stack.Push(nextIndex);
            _logger.LogDebug("[{ChatId}] Переход с шага {CurrentIndex} на {NextIndex}", chatId, currentIndex, nextIndex);
            await _steps[nextIndex].Ask(chatId, builder);
        }
        public async Task GoBack(long chatId)
        {
            if (!_playerbuilder.TryGetValue(chatId, out var builder))
            {
                _logger.LogWarning("[{ChatId}] Билдер не найден при GoBack", chatId);
                return;
            }
            if (!_stepHistory.TryGetValue(chatId, out var stack) || stack.Count <= 1)
            {
                _logger.LogWarning("[{ChatId}] Нельзя вернуться назад (нет предыдущего шага)", chatId);
                await _botClient.SendTextMessageAsync(chatId, "❌ Нельзя вернуться назад.");
                return;
            }
            stack.Pop();
            int previousIndex = stack.Peek();
            _logger.LogDebug("[{ChatId}] Возврат на шаг {PreviousIndex}", chatId, previousIndex);
            await _steps[previousIndex].Ask(chatId, builder);
        }
        public async Task HandleInput(long chatId, string data)
        {
            if (!_playerbuilder.TryGetValue(chatId, out var builder))
            {
                _logger.LogWarning("[{ChatId}] Билдер не найден при HandleInput", chatId);
                return;
            }
            if (!_stepHistory.TryGetValue(chatId, out var stack) || stack.Count == 0)
            {
                _logger.LogWarning("[{ChatId}] Стек истории пуст", chatId);
                return;
            }
            int currentIndex = stack.Peek();
            var step = _steps[currentIndex];
            _logger.LogDebug("[{ChatId}] Текущий шаг: {StepType}, данные: {Data}", chatId, step.GetType().Name, data);

            if (data == "back")
            {
                await GoBack(chatId);
                return;
            }

            if (step.CanHandle(data))
            {
                await step.Handle(chatId, builder, data);
            }
            else
            {
                _logger.LogWarning("[{ChatId}] Шаг {StepType} не может обработать данные: {Data}", chatId, step.GetType().Name, data);
                await _botClient.SendTextMessageAsync(chatId, "❌ Пожалуйста, следуйте инструкциям.");
            }
        }

        public async Task HandleCallback(long chatId, CallbackQuery callbackQuery)
        {
            if (!_playerbuilder.ContainsKey(chatId)) return;
            await HandleInput(chatId, callbackQuery.Data);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        public async Task RestartCreation(long chatId)
        {
            _logger.LogDebug("Перезапуск создания персонажа для {ChatId}", chatId);
            _playerbuilder.Remove(chatId);
            _stepHistory.Remove(chatId);
            StartCreation(chatId);
        }

        public async Task CompleteCreation(long chatId)
        {
            if (!_playerbuilder.TryGetValue(chatId, out var builder)) return;
            var player = builder.Build();
            await _databaseService.SavePlayerAsync(player);
            _playerManager.AddOrUpdatePlayer(player);
            _playerbuilder.Remove(chatId);
            _stepHistory.Remove(chatId);
            await _botClient.SendTextMessageAsync(chatId,
                "🎊 *Добро пожаловать в мир Аркадии!*\n\nВаше приключение начинается...",
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardHelper.GetMovementKeyboard());
            await _locationService.DescribeLocation(chatId, player);
        }
    }
}