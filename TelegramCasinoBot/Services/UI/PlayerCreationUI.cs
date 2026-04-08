using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Models.Stats;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.UI
{
    public class PlayerCreationUI
    {
        private readonly TelegramBotClient _botClient;
        private readonly IRaceService _raceService;
        private readonly IClassService _classService;
        private readonly CharacterIconService _iconService;
        private readonly DatabaseService _databaseService;
        private readonly PlayerManager _playerManager;
        private readonly ILogger<PlayerCreationUI> _logger;

        private readonly Dictionary<long, (string Name, string Gender, Race Race, Class Class, string IconPath)> _creationData = new();

        public PlayerCreationUI(
            TelegramBotClient botClient,
            IRaceService raceService,
            IClassService classService,
            CharacterIconService iconService,
            DatabaseService databaseService,
            PlayerManager playerManager,
            ILogger<PlayerCreationUI> logger)
        {
            _botClient = botClient;
            _raceService = raceService;
            _classService = classService;
            _iconService = iconService;
            _databaseService = databaseService;
            _playerManager = playerManager;
            _logger = logger;
        }

        public bool IsInCharacterCreation(long chatId) => _creationData.ContainsKey(chatId);
        public Player GetCharacterInProgress(long chatId)
        {
            if (_creationData.TryGetValue(chatId, out var data))
            {
                var tempPlayer = new Player(chatId, data.Name, data.Gender, data.Race, data.Class, data.IconPath, (100, 50, 100, 10, 0, 1));
                return tempPlayer;
            }
            return null;
        }
        public void StartCreation(long chatId)
        {
            _logger.LogDebug("Начало создания персонажа для {ChatId}", chatId);
            _creationData[chatId] = (null, null, null, null, null);
            _ = AskName(chatId);
        }

        private async Task AskName(long chatId)
        {
            await _botClient.SendTextMessageAsync(chatId,
                "🎮 *СОЗДАНИЕ ПЕРСОНАЖА*\n\nКак зовут вашего героя?",
                parseMode: ParseMode.Markdown,
                replyMarkup: new ReplyKeyboardRemove());
        }

        public async Task HandleName(long chatId, string name)
        {
            if (_creationData.TryGetValue(chatId, out var data))
            {
                data.Name = name;
                _creationData[chatId] = data;
                await AskGender(chatId);
            }
        }

        private async Task AskGender(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "👨 Мужской", "👩 Женский" },
                new KeyboardButton[] { "🔙 Назад" }
            })
            { ResizeKeyboard = true };
            await _botClient.SendTextMessageAsync(chatId, "Выберите пол вашего персонажа:", replyMarkup: keyboard);
        }

        public async Task HandleGender(long chatId, string gender)
        {
            string selected = gender.Contains("Мужской") ? "Male" : gender.Contains("Женский") ? "Female" : null;
            if (selected == null) return;
            if (_creationData.TryGetValue(chatId, out var data))
            {
                data.Gender = selected;
                _creationData[chatId] = data;
                await AskRace(chatId);
            }
        }

        private async Task AskRace(long chatId)
        {
            var races = await _raceService.GetAllRacesAsync();
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (Race race in races)
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(race.Name, $"race_{race.Id}") });
            var keyboard = new InlineKeyboardMarkup(buttons);
            await _botClient.SendTextMessageAsync(chatId,
                "🎯 *ВЫБОР РАСЫ*\n\nВыберите расу вашего персонажа:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public async Task HandleRace(long chatId, string callbackData)
        {
            if (!callbackData.StartsWith("race_")) return;
            if (!int.TryParse(callbackData.Substring(5), out int raceId)) return;

            var race = await _raceService.GetRaceByIdAsync(raceId);
            if (race == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка выбора расы.");
                return;
            }

            if (_creationData.TryGetValue(chatId, out var data))
            {
                data.Race = race;
                _creationData[chatId] = data;
                await AskClass(chatId);
            }
        }

        private async Task AskClass(long chatId)
        {
            var classes = await _classService.GetAllClassesAsync();
            var buttons = new List<InlineKeyboardButton[]>();
            foreach (Class cls in classes)
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(cls.Name, $"class_{cls.Id}") });
            var keyboard = new InlineKeyboardMarkup(buttons);
            await _botClient.SendTextMessageAsync(chatId,
                "🎯 *ВЫБОР КЛАССА*\n\nВыберите класс вашего персонажа:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public async Task HandleClass(long chatId, string callbackData)
        {
            if (!callbackData.StartsWith("class_")) return;
            if (!int.TryParse(callbackData.Substring(6), out int classId)) return;

            var playerClass = await _classService.GetClassByIdAsync(classId);
            if (playerClass == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка выбора класса.");
                return;
            }

            if (_creationData.TryGetValue(chatId, out var data))
            {
                data.Class = playerClass;
                _creationData[chatId] = data;
                await StartIconSelection(chatId);
            }
        }

        private async Task StartIconSelection(long chatId)
        {
            if (_creationData.TryGetValue(chatId, out var data))
            {
                await _botClient.SendTextMessageAsync(chatId, "🎨 Теперь выберите внешность вашего персонажа!", parseMode: ParseMode.Markdown);
                await _iconService.StartIconSelection(chatId, data.Gender, data.Race.Name);
            }
        }

        public async Task HandleIconConfirmation(long chatId)
        {
            var iconPath = _iconService.GetSelectedIconPath(chatId);
            if (!string.IsNullOrEmpty(iconPath) && _creationData.TryGetValue(chatId, out var data))
            {
                data.IconPath = iconPath;
                _creationData[chatId] = data;
                await ShowSummary(chatId);
            }
            _iconService.ClearSelection(chatId);
        }

        private async Task ShowSummary(long chatId)
        {
            if (!_creationData.TryGetValue(chatId, out var data)) return;

            var tempPlayer = new Player(chatId, data.Name, data.Gender, data.Race, data.Class, data.IconPath);

            var summary = $@"🎉 *ПЕРСОНАЖ СОЗДАН!*

*Имя:* {tempPlayer.Name}
*Пол:* {(tempPlayer.Gender == "Male" ? "👨 Мужской" : "👩 Женский")}
*Раса:* {tempPlayer.Race}
*Класс:* {tempPlayer.Class}

*Характеристики:*
❤️ Здоровье: {tempPlayer.Health}/{tempPlayer.MaxHealth}
🔮 Мана: {tempPlayer.Mana}/{tempPlayer.MaxMana}
💪 Выносливость: {tempPlayer.Stamina}/{tempPlayer.MaxStamina}
🛡️ Защита: {tempPlayer.Defense}

*Бонусы:*
⭐ Множитель опыта: {Math.Round(tempPlayer.ExperienceMultiplier * 100, 1)}%
⚔️ Ближний урон: {Math.Round(tempPlayer.MeleeDamageMultiplier * 100, 1)}%
🏹 Дальний урон: {Math.Round(tempPlayer.RangedDamageMultiplier * 100, 1)}%
🔮 Магический урон: {Math.Round(tempPlayer.MagicDamageMultiplier * 100, 1)}%

*Способности:* {string.Join(", ", tempPlayer.Abilities)}

Готовы начать приключение?";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✅ Начать игру", "confirm_character") },
                new[] { InlineKeyboardButton.WithCallbackData("🔁 Пересоздать", "restart_character") }
            });
            await _botClient.SendTextMessageAsync(chatId, summary, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        public async Task CompleteCreation(long chatId)
        {
            if (!_creationData.TryGetValue(chatId, out var data)) return;

            var player = new Player(chatId, data.Name, data.Gender, data.Race, data.Class, data.IconPath);

            await _databaseService.SavePlayerAsync(player);
            _playerManager.AddOrUpdatePlayer(player);
            _creationData.Remove(chatId);

            await _botClient.SendTextMessageAsync(chatId,
                "🎊 *Добро пожаловать в мир Аркадии!*\n\nВаше приключение начинается...",
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardHelper.GetMovementKeyboard());
        }
    }
}