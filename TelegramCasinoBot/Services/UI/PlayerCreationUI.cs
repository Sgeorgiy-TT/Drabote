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
using TelegramCasinoBot.Services.UI.Steps.Dispatcher;
using TelegramCasinoBot.Services.UI.Steps.StepsCreation;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.UI
{
    public class PlayerCreationUI
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<PlayerCreationUI> _logger;
        private readonly List<ICreationStep> _steps;
        private readonly StepDispatcher _stepDispatcher;
        private readonly DatabaseService _databaseService;
        private readonly PlayerManager _playerManager;
        private readonly Dictionary<long, int> _currentStepIndex = new();
        private readonly Dictionary<long, Player.PlayerBuilder> _playerbuilder = new();
        private readonly AbilityService _abilityService;
        private readonly ImageService _imageService;
        public IReadOnlyList<ICreationStep> Steps => _steps;

        public PlayerCreationUI(
            TelegramBotClient botClient,
            ILogger<PlayerCreationUI> logger,
            IRaceService raceService,
            IClassService classService,
            CharacterIconService characterIconService,
            DatabaseService databaseService,      
            PlayerManager playerManager)
        {
            _botClient = botClient;
            _databaseService = databaseService;
            _playerManager = playerManager;
            _logger = logger;

            _steps = new List<ICreationStep>
            {
                new NameStep(botClient, async chatId => await NextStep(chatId), async chatId => await RestartCreation(chatId)),
                new GenderStep(botClient, async chatId => await NextStep(chatId), async chatId => await RestartCreation(chatId)),
                new RaceStep(botClient, raceService, async chatId => await NextStep(chatId), async chatId => await RestartCreation(chatId)),
                new ClassStep(botClient, classService, async chatId => await NextStep(chatId), async chatId => await RestartCreation(chatId)),
                new IconStep(botClient, characterIconService, async chatId => await NextStep(chatId), async chatId => await RestartCreation(chatId)),
                new SummaryStep(botClient, async chatId => await NextStep(chatId), async chatId => await RestartCreation(chatId))
            };
        }
        public bool IsInCharacterCreation(long chatId) => _playerbuilder.ContainsKey(chatId);
        //второй интерфейс чтобы он переиспользовал логику PlayerCreationUI базовый класс для шагов
        public void StartCreation(long chatId)
        {
            _logger.LogDebug("Начало создания персонажа для {ChatId}", chatId);
            var builder = new Player.PlayerBuilder(_imageService, _abilityService);
            builder.SetChatId(chatId);
            _playerbuilder[chatId] = builder;
            _currentStepIndex[chatId] = 0;
            _ = NextStep(chatId);
        }
        //метод который вызывает в нужной последовательности и все обработчики
        public async Task NextStep(long chatId)//NextStep переименовать
        {
            if (!_playerbuilder.ContainsKey(chatId)) return;
            var builder = _playerbuilder[chatId];
            if (!_currentStepIndex.TryGetValue(chatId, out int index)) return;

            int nextIndex = index + 1;
            if (nextIndex >= _steps.Count)
            {
                await CompleteCreation(chatId);
                return;
            }

            _currentStepIndex[chatId] = nextIndex;
            await _steps[nextIndex].Ask(chatId, builder);
        }

        public async Task HandleInput(long chatId, string data)
        {
            if (!_playerbuilder.ContainsKey(chatId)) return;
            await _stepDispatcher.Dispatch(chatId, data);
        }
        public async Task HandleCallback(long chatId, CallbackQuery callbackQuery)
        {
            if (!_playerbuilder.ContainsKey(chatId)) return;
            var data = callbackQuery.Data;
            await HandleInput(chatId, data);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        public async Task RestartCreation(long chatId)
        {
            _logger.LogDebug("Перезапуск создания персонажа для {ChatId}", chatId);
            _playerbuilder.Remove(chatId);
            _currentStepIndex.Remove(chatId);
            StartCreation(chatId);
        }

        public async Task CompleteCreation(long chatId)
        {
            if (!_playerbuilder.TryGetValue(chatId, out var builder)) return;

            var player = builder.Build();
            await _databaseService.SavePlayerAsync(player);
            _playerManager.AddOrUpdatePlayer(player);
            _playerbuilder.Remove(chatId);
            _currentStepIndex.Remove(chatId);

            await _botClient.SendTextMessageAsync(chatId,
                "🎊 *Добро пожаловать в мир Аркадии!*\n\nВаше приключение начинается...",
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardHelper.GetMovementKeyboard());
        }
    }
}