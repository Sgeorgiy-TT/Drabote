using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Models.Stats;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Data;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI;
using TelegramCasinoBot.Utils;
using TelegramMetroidvaniaBot;

namespace TelegramCasinoBot.Services.Models.Gameplay
{
    public class CharacterCreationService
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseService _databaseService;
        private readonly CharacterIconService _iconService;
        private readonly IRaceService _raceService;
        private readonly IClassService _classService;
        private readonly LocationService _locationService;
        private readonly GameWorld _world;
        private readonly ILogger<CharacterCreationService> _logger;
        private readonly Dictionary<long, Player> _characterCreationProgress = new Dictionary<long, Player>();

        public CharacterCreationService(
            TelegramBotClient botClient,
            DatabaseService databaseService,
            CharacterIconService iconService,
            IRaceService raceService,
            IClassService classService,
            LocationService locationService,  
            GameWorld world,
            ILogger<CharacterCreationService> logger = null)
        {
            _botClient = botClient;
            _databaseService = databaseService;
            _iconService = iconService;
            _raceService = raceService;
            _classService = classService;
            _locationService = locationService;
            _world = world;
            _logger = logger ?? NullLogger<CharacterCreationService>.Instance;
        }

        public async Task StartCharacterCreation(long chatId)
        {
            _logger.LogDebug("Начало StartCharacterCreation для chatId {ChatId}", chatId);
            try
            {
                var newPlayer = new Player(chatId);
                _characterCreationProgress[chatId] = newPlayer;
                await AskForName(chatId);
            }
            finally
            {
                _logger.LogDebug("StartCharacterCreation завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task AskForName(long chatId)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🎮 *СОЗДАНИЕ ПЕРСОНАЖА*\n\nКак зовут вашего героя?",
                parseMode: ParseMode.Markdown,
                replyMarkup: new ReplyKeyboardRemove());
        }

        public async Task HandleNameInput(long chatId, string name)
        {
            _logger.LogDebug("Начало HandleNameInput для chatId {ChatId}", chatId);
            try
            {
                if (_characterCreationProgress.ContainsKey(chatId))
                {
                    _characterCreationProgress[chatId].Name = name;
                    await AskForGender(chatId);
                }
            }
            finally
            {
                _logger.LogDebug("HandleNameInput завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task AskForGender(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "👨 Мужской", "👩 Женский" },
                new KeyboardButton[] { "🔙 Назад" }
            })
            {
                ResizeKeyboard = true
            };

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Выберите пол вашего персонажа:",
                replyMarkup: keyboard);
        }

        public async Task HandleGenderInput(long chatId, string gender)
        {
            _logger.LogDebug("Начало HandleGenderInput для chatId {ChatId}", chatId);
            try
            {
                if (_characterCreationProgress.ContainsKey(chatId))
                {
                    var player = _characterCreationProgress[chatId];

                    if (gender.Contains("Мужской"))
                        player.Gender = "Male";
                    else if (gender.Contains("Женский"))
                        player.Gender = "Female";
                    else
                        return;

                    await AskForRace(chatId);
                }
            }
            finally
            {
                _logger.LogDebug("HandleGenderInput завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task AskForRace(long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("👤 Человек", "race_human"),
                    InlineKeyboardButton.WithCallbackData("🧝 Эльф", "race_elf")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("👹 Орк", "race_orc"),
                    InlineKeyboardButton.WithCallbackData("🧔 Гном", "race_dwarf")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🐲 Драконид", "race_dragonkin")
                }
            });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🎯 *ВЫБОР РАСЫ*\n\nВыберите расу вашего персонажа:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public async Task HandleRaceSelection(long chatId, string raceId)
        {
            _logger.LogDebug("Начало HandleRaceSelection для chatId {ChatId}, raceId {RaceId}", chatId, raceId);
            try
            {
                if (!_characterCreationProgress.ContainsKey(chatId))
                    return;

                var race = _raceService.GetRaceById(raceId);
                if (race == null)
                {
                    _logger.LogWarning("Раса с Id {RaceId} не найдена", raceId);
                    return;
                }

                var player = _characterCreationProgress[chatId];
                ApplyRaceBonuses(player, race);
                await AskForClass(chatId);
            }
            finally
            {
                _logger.LogDebug("HandleRaceSelection завершён для chatId {ChatId}", chatId);
            }
        }

        private void ApplyRaceBonuses(Player player, Race race)
        {
            player.Race = race.Name;
            player.MaxHealth = MathHelper.Clamp(player.MaxHealth + race.HealthBonus, 50, 1000);
            player.Health = player.MaxHealth;
            player.MaxMana = MathHelper.Clamp(player.MaxMana + race.ManaBonus, 20, 500);
            player.Mana = player.MaxMana;
            player.MaxStamina = MathHelper.Clamp(player.MaxStamina + race.StaminaBonus, 50, 300);
            player.Stamina = player.MaxStamina;
            player.Defense = MathHelper.Clamp(player.Defense + race.DefenseBonus, 0, 100);
            player.ExperienceMultiplier = MathHelper.Clamp(race.ExperienceMultiplier, 0.5, 2.0);
            player.MeleeDamageMultiplier = MathHelper.Clamp(race.MeleeDamageMultiplier, 0.5, 2.0);
            player.RangedDamageMultiplier = MathHelper.Clamp(race.RangedDamageMultiplier, 0.5, 2.0);
            player.MagicDamageMultiplier = MathHelper.Clamp(race.MagicDamageMultiplier, 0.5, 2.0);

            foreach (var ability in race.SpecialAbilities)
            {
                player.Abilities.Add(ability);
            }
            player.CharacterStatsList.Add(race);
        }

        private async Task AskForClass(long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⚔️ Воин", "class_warrior"),
                    InlineKeyboardButton.WithCallbackData("🏹 Лучник", "class_archer")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔮 Маг", "class_mage")
                }
            });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🎯 *ВЫБОР КЛАССА*\n\nВыберите класс вашего персонажа:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public async Task HandleClassSelection(long chatId, string classId)
        {
            _logger.LogDebug("Начало HandleClassSelection для chatId {ChatId}, classId {ClassId}", chatId, classId);
            try
            {
                if (!_characterCreationProgress.ContainsKey(chatId))
                    return;

                var characterClass = _classService.GetClassById(classId);
                if (characterClass == null)
                {
                    _logger.LogWarning("Класс с Id {ClassId} не найден", classId);
                    return;
                }

                var player = _characterCreationProgress[chatId];
                ApplyClassBonuses(player, characterClass);
                await StartIconSelection(chatId);
            }
            finally
            {
                _logger.LogDebug("HandleClassSelection завершён для chatId {ChatId}", chatId);
            }
        }

        private void ApplyClassBonuses(Player player, Class characterClass)
        {
            player.Class = characterClass.Name; 
            player.MaxHealth = MathHelper.Clamp(player.MaxHealth + characterClass.HealthBonus, 50, 1000);
            player.Health = player.MaxHealth;
            player.MaxMana = MathHelper.Clamp(player.MaxMana + characterClass.ManaBonus, 20, 500);
            player.Mana = player.MaxMana;
            player.MaxStamina = MathHelper.Clamp(player.MaxStamina + characterClass.StaminaBonus, 50, 300);
            player.Stamina = player.MaxStamina;
            player.Defense = MathHelper.Clamp(player.Defense + characterClass.DefenseBonus, 0, 100);

            player.MeleeDamageMultiplier = MathHelper.Clamp(
                player.MeleeDamageMultiplier * characterClass.MeleeDamageMultiplier, 0.5, 3.0);
            player.RangedDamageMultiplier = MathHelper.Clamp(
                player.RangedDamageMultiplier * characterClass.RangedDamageMultiplier, 0.5, 3.0);
            player.MagicDamageMultiplier = MathHelper.Clamp(
                player.MagicDamageMultiplier * characterClass.MagicDamageMultiplier, 0.5, 3.0);

            foreach (var ability in characterClass.StartingAbilities)
            {
                player.Abilities.Add(ability);
            }
            player.CharacterStatsList.Add(characterClass);
        }

        public async Task StartIconSelection(long chatId)
        {
            _logger.LogDebug("Начало StartIconSelection для chatId {ChatId}", chatId);
            try
            {
                if (_characterCreationProgress.ContainsKey(chatId))
                {
                    var player = _characterCreationProgress[chatId];

                    if (_iconService == null)
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❌ Ошибка системы выбора иконок. Продолжаем без выбора внешности.");
                        await ShowCharacterSummary(chatId);
                        return;
                    }

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "🎨 Теперь выберите внешность вашего персонажа!",
                        parseMode: ParseMode.Markdown);

                    await _iconService.StartIconSelection(chatId, player.Gender, player.Race);
                }
            }
            finally
            {
                _logger.LogDebug("StartIconSelection завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task HandleIconConfirmation(long chatId)
        {
            _logger.LogDebug("Начало HandleIconConfirmation для chatId {ChatId}", chatId);
            try
            {
                if (_characterCreationProgress.ContainsKey(chatId))
                {
                    if (_iconService != null)
                    {
                        var iconPath = _iconService.GetSelectedIconPath(chatId);
                        var player = _characterCreationProgress[chatId];
                        player.IconPath = iconPath;
                        _iconService.ClearSelection(chatId);
                    }

                    await ShowCharacterSummary(chatId);
                }
            }
            finally
            {
                _logger.LogDebug("HandleIconConfirmation завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task ShowCharacterSummary(long chatId)
        {
            if (_characterCreationProgress.ContainsKey(chatId))
            {
                var player = _characterCreationProgress[chatId];

                var summary = $@"🎉 *ПЕРСОНАЖ СОЗДАН!*

*Имя:* {player.Name}
*Пол:* {(player.Gender == "Male" ? "👨 Мужской" : "👩 Женский")}
*Раса:* {player.Race}
*Класс:* {player.Class}

*Характеристики:*
❤️ Здоровье: {player.Health}/{player.MaxHealth}
🔮 Мана: {player.Mana}/{player.MaxMana}
💪 Выносливость: {player.Stamina}/{player.MaxStamina}
🛡️ Защита: {player.Defense}

*Бонусы:*
⭐ Множитель опыта: {Math.Round(player.ExperienceMultiplier * 100, 1)}%
⚔️ Ближний урон: {Math.Round(player.MeleeDamageMultiplier * 100, 1)}%
🏹 Дальний урон: {Math.Round(player.RangedDamageMultiplier * 100, 1)}%
🔮 Магический урон: {Math.Round(player.MagicDamageMultiplier * 100, 1)}%

*Способности:* {string.Join(", ", player.Abilities)}

Готовы начать приключение?";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Начать игру", "confirm_character"),
                        InlineKeyboardButton.WithCallbackData("🔁 Пересоздать", "restart_character")
                    }
                });

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: summary,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard);
            }
        }

        public async Task CompleteCharacterCreation(long chatId)
        {
            _logger.LogDebug("Начало CompleteCharacterCreation для chatId {ChatId}", chatId);
            try
            {
                if (_characterCreationProgress.ContainsKey(chatId))
                {
                    var player = _characterCreationProgress[chatId];

                    player.CurrentLocation = "start";
                    player.PositionX = 5;
                    player.PositionY = 5;

                    await _databaseService.SavePlayerAsync(player);

                    if (Program.Players.ContainsKey(chatId))
                    {
                        Program.Players[chatId] = player;
                    }
                    else
                    {
                        Program.Players.Add(chatId, player);
                    }

                    _characterCreationProgress.Remove(chatId);

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "🎊 *Добро пожаловать в мир Аркадии!*\n\nВаше приключение начинается...",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: GetGameKeyboard());

                    await _locationService.DescribeLocation(chatId, player);
                }
            }
            finally
            {
                _logger.LogDebug("CompleteCharacterCreation завершён для chatId {ChatId}", chatId);
            }
        }

        private ReplyKeyboardMarkup GetGameKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "⬆️ Север", "⬇️ Юг" },
                new KeyboardButton[] { "⬅️ Запад", "➡️ Восток" },
                new KeyboardButton[] { "🗺️ Карта мира", "🎒 Инвентарь", "📊 Статус" },
                new KeyboardButton[] { "🔍 Осмотреть", "💬 Поговорить", "⚔️ Атаковать" }
            })
            {
                ResizeKeyboard = true
            };
        }

        public bool IsInCharacterCreation(long chatId)
        {
            return _characterCreationProgress.ContainsKey(chatId);
        }

        public Player GetCharacterInProgress(long chatId)
        {
            return _characterCreationProgress.ContainsKey(chatId) ? _characterCreationProgress[chatId] : null;
        }
    }
}