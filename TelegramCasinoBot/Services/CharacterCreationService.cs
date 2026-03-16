using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMetroidvaniaBot.Models;
using TelegramMetroidvaniaBot.Utils;

namespace TelegramMetroidvaniaBot.Services
{
    public class CharacterCreationService
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseService _databaseService;
        private readonly CharacterIconService _iconService;
        private readonly ILogger<CharacterCreationService> _logger;
        private readonly Dictionary<long, Player> _characterCreationProgress = new Dictionary<long, Player>();

        public CharacterCreationService(TelegramBotClient botClient, DatabaseService databaseService,
            CharacterIconService iconService, ILogger<CharacterCreationService> logger = null)
        {
            _botClient = botClient;
            _databaseService = databaseService;
            _iconService = iconService;
            _logger = logger ?? NullLogger<CharacterCreationService>.Instance;
        }

        private readonly Dictionary<string, Race> _races = new Dictionary<string, Race>
        {
            ["human"] = new Race
            {
                Id = "human",
                Name = "Человек",
                Description = "Универсальная раса с балансом всех характеристик",
                HealthBonus = 0,
                ManaBonus = 0,
                StaminaBonus = 0,
                DefenseBonus = 0,
                ExperienceMultiplier = 1.1,
                MeleeDamageBonus = 0,
                RangedDamageBonus = 0,
                MagicDamageBonus = 0,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new[] { "Адаптивность" }
            },
            ["elf"] = new Race
            {
                Id = "elf",
                Name = "Эльф",
                Description = "Древняя раса с affinity к магии",
                HealthBonus = -10,
                ManaBonus = 50,
                StaminaBonus = 0,
                DefenseBonus = 0,
                ExperienceMultiplier = 1.0,
                MeleeDamageBonus = 0,
                RangedDamageBonus = 0,
                MagicDamageBonus = 1.05,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new[] { "Магическая affinity" }
            },
            ["orc"] = new Race
            {
                Id = "orc",
                Name = "Орк",
                Description = "Сильная и выносливая раса",
                HealthBonus = 20,
                ManaBonus = -20,
                StaminaBonus = 10,
                DefenseBonus = 0,
                ExperienceMultiplier = 1.0,
                MeleeDamageBonus = 1.1,
                RangedDamageBonus = 0,
                MagicDamageBonus = 0,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new[] { "Берсерк" }
            },
            ["dwarf"] = new Race
            {
                Id = "dwarf",
                Name = "Гном",
                Description = "Крепкие и устойчивые бойцы",
                HealthBonus = 10,
                ManaBonus = 0,
                StaminaBonus = 0,
                DefenseBonus = 20,
                ExperienceMultiplier = 1.0,
                MeleeDamageBonus = 0,
                RangedDamageBonus = 0,
                MagicDamageBonus = 0,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new[] { "Устойчивость" }
            },
            ["dragonkin"] = new Race
            {
                Id = "dragonkin",
                Name = "Драконид",
                Description = "Потомки древних драконов",
                HealthBonus = 0,
                ManaBonus = 20,
                StaminaBonus = 0,
                DefenseBonus = 10,
                ExperienceMultiplier = 1.0,
                MeleeDamageBonus = 0,
                RangedDamageBonus = 0,
                MagicDamageBonus = 0,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new[] { "Огненный шар" }
            }
        };

        private readonly Dictionary<string, CharacterClass> _classes = new Dictionary<string, CharacterClass>
        {
            ["warrior"] = new CharacterClass
            {
                Id = "warrior",
                Name = "Воин",
                Description = "Мастер ближнего боя",
                HealthBonus = 20,
                ManaBonus = 0,
                StaminaBonus = 20,
                DefenseBonus = 10,
                MeleeDamageMultiplier = 1.1,
                RangedDamageMultiplier = 1.0,
                MagicDamageMultiplier = 1.0,
                StartingAbilities = new[] { "Обычный удар", "Усиленный удар" },
                PreferredWeaponTypes = new[] { "Меч", "Топор", "Булава" }
            },
            ["archer"] = new CharacterClass
            {
                Id = "archer",
                Name = "Лучник",
                Description = "Стрелок на дальних дистанциях",
                HealthBonus = 0,
                ManaBonus = 10,
                StaminaBonus = 10,
                DefenseBonus = 5,
                MeleeDamageMultiplier = 1.0,
                RangedDamageMultiplier = 1.1,
                MagicDamageMultiplier = 1.0,
                StartingAbilities = new[] { "Обычный выстрел", "Усиленный выстрел" },
                PreferredWeaponTypes = new[] { "Лук", "Арбалет" }
            },
            ["mage"] = new CharacterClass
            {
                Id = "mage",
                Name = "Маг",
                Description = "Повелитель магических искусств",
                HealthBonus = -20,
                ManaBonus = 30,
                StaminaBonus = 0,
                DefenseBonus = 0,
                MeleeDamageMultiplier = 1.0,
                RangedDamageMultiplier = 1.0,
                MagicDamageMultiplier = 1.1,
                StartingAbilities = new[] { "Магический выстрел", "Усиленный магический выстрел" },
                PreferredWeaponTypes = new[] { "Посох", "Жезл", "Книга заклинаний" }
            }
        };

        public async Task StartCharacterCreation(long chatId)
        {
            var newPlayer = new Player
            {
                ChatId = chatId,
                Health = 100,
                MaxHealth = 100,
                Mana = 50,
                MaxMana = 50,
                Stamina = 100,
                MaxStamina = 100,
                Defense = 10,
                Experience = 0,
                Level = 1
            };

            _characterCreationProgress[chatId] = newPlayer;
            await AskForName(chatId);
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
            if (_characterCreationProgress.ContainsKey(chatId))
            {
                _characterCreationProgress[chatId].Name = name;
                await AskForGender(chatId);
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
            if (_characterCreationProgress.ContainsKey(chatId) && _races.ContainsKey(raceId))
            {
                var player = _characterCreationProgress[chatId];
                var race = _races[raceId];

                player.Race = race.Name;

                player.MaxHealth = MathHelper.Clamp(player.MaxHealth + race.HealthBonus, 50, 1000);
                player.Health = player.MaxHealth;
                player.MaxMana = MathHelper.Clamp(player.MaxMana + race.ManaBonus, 20, 500);
                player.Mana = player.MaxMana;
                player.MaxStamina = MathHelper.Clamp(player.MaxStamina + race.StaminaBonus, 50, 300);
                player.Stamina = player.MaxStamina;
                player.Defense = MathHelper.Clamp(player.Defense + race.DefenseBonus, 0, 100);
                player.ExperienceMultiplier = MathHelper.Clamp(race.ExperienceMultiplier, 0.5, 2.0);
                player.MeleeDamageMultiplier = MathHelper.Clamp(race.MeleeDamageBonus, 0.5, 2.0);
                player.RangedDamageMultiplier = MathHelper.Clamp(race.RangedDamageBonus, 0.5, 2.0);
                player.MagicDamageMultiplier = MathHelper.Clamp(race.MagicDamageBonus, 0.5, 2.0);

                foreach (var ability in race.SpecialAbilities)
                {
                    player.Abilities.Add(ability);
                }

                await AskForClass(chatId);
            }
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
            if (_characterCreationProgress.ContainsKey(chatId) && _classes.ContainsKey(classId))
            {
                var player = _characterCreationProgress[chatId];
                var characterClass = _classes[classId];

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

                await StartIconSelection(chatId);
            }
        }

        public async Task StartIconSelection(long chatId)
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

        public async Task HandleIconConfirmation(long chatId)
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

                var locationService = new LocationService(_botClient, new GameWorld());
                await locationService.DescribeLocation(chatId, player);
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