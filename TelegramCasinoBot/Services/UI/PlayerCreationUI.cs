using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.UI.Steps;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.UI
{
    public class PlayerCreationUI
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseService _databaseService;
        private readonly PlayerManager _playerManager;
        private readonly ILogger<PlayerCreationUI> _logger;
        private readonly List<ICreationStep> _steps;

        private readonly Dictionary<long, int> _currentStepIndex = new();
        private readonly Dictionary<long, Player.PlayerBuilder> _creationData = new();

        public PlayerCreationUI(
            TelegramBotClient botClient,
        DatabaseService databaseService,
        PlayerManager playerManager,
        ILogger<PlayerCreationUI> logger,
        IRaceService raceService,
        IClassService classService,
        CharacterIconService characterIconService)
        {
            _botClient = botClient;
            _databaseService = databaseService;
            _playerManager = playerManager;
            _logger = logger;
            _steps = new List<ICreationStep>
        {
            new NameStep(botClient, this),
            new GenderStep(botClient, this),
            new RaceStep(botClient, this, raceService),
            new ClassStep(botClient, this, classService),
            new IconStep(botClient, this, characterIconService),
            new SummaryStep(botClient, this)
        };
        }

        public bool IsInCharacterCreation(long chatId) => _creationData.ContainsKey(chatId);

        public void StartCreation(long chatId)
        {
            _logger.LogDebug("Начало создания персонажа для {ChatId}", chatId);
            _creationData[chatId] = new Player.PlayerBuilder();
            _currentStepIndex[chatId] = 0;
            _ = NextStep(chatId);//написать серию вызова
        }
        //попробовать заменить вызовы на использование обьектов
        //методы акс в какомто порядке разложить
        public async Task NextStep(long chatId)
        {
            if (!_creationData.TryGetValue(chatId, out var builder)) return;
            if (!_currentStepIndex.TryGetValue(chatId, out int index)) return;

            if (index >= _steps.Count)
            {
                await CompleteCreation(chatId);
                return;
            }

            var step = _steps[index];
            await step.Ask(chatId);
        }

        public async Task HandleInput(long chatId, string messageText)
        {
            if (!_creationData.TryGetValue(chatId, out var builder)) return;
            if (!_currentStepIndex.TryGetValue(chatId, out int index)) return;

            var step = _steps[index];
            if (step.CanHandle(messageText))
            {
                await step.Handle(chatId, messageText);
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Пожалуйста, следуйте инструкциям.");
            }
        }

        public void SetName(long chatId, string name)
        {
            if (_creationData.TryGetValue(chatId, out var builder))
                builder.SetName(name);
        }

        public void SetGender(long chatId, string gender)
        {
            if (_creationData.TryGetValue(chatId, out var builder))
                builder.SetGender(gender);
        }

        public void SetRace(long chatId, Race race)
        {
            if (_creationData.TryGetValue(chatId, out var builder))
                builder.SetRace(race);
        }

        public void SetClass(long chatId, Class cls)
        {
            if (_creationData.TryGetValue(chatId, out var builder))
                builder.SetClass(cls);
        }

        public void SetIconPath(long chatId, string iconPath)
        {
            if (_creationData.TryGetValue(chatId, out var builder))
                builder.SetIconName(iconPath);
        }

        public string GetGender(long chatId)
        {
            return _creationData.TryGetValue(chatId, out var builder) ? builder.GetGender() : null;
        }

        public string GetRaceName(long chatId)
        {
            return _creationData.TryGetValue(chatId, out var builder) ? builder.GetRace()?.Name : null;
        }
        public Player GetTempPlayer(long chatId)
        {
            if (!_creationData.TryGetValue(chatId, out var builder)) return null;
            return builder.Build();
        }
        public async Task CompleteCreation(long chatId)
        {
            if (!_creationData.TryGetValue(chatId, out var builder)) return;

            var player = builder.Build();
            await _databaseService.SavePlayerAsync(player);
            _playerManager.AddOrUpdatePlayer(player);
            _creationData.Remove(chatId);
            _currentStepIndex.Remove(chatId);

            await _botClient.SendTextMessageAsync(chatId,
                "🎊 *Добро пожаловать в мир Аркадии!*\n\nВаше приключение начинается...",
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardHelper.GetMovementKeyboard());
        }
    }
}