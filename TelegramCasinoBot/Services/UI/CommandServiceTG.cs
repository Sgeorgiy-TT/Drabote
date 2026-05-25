using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Infrastructure.Location;
using TelegramCasinoBot.Services.Models.DataStats;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI.Handlers;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.UI
{
    public class CommandServiceTG
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly MovementService _movementService;
        private readonly LocationService _locationService;
        private readonly MapService _mapService;
        private readonly InventoryService _inventoryService;
        private readonly ILogger<CommandServiceTG> _logger;
        private readonly BattleService _battleService;
        private readonly MenuServiceTG _menuService;
        private readonly ItemService _itemService;
        private readonly TraderHandler _traderHandler;
        private readonly DatabaseService _databaseService;
        public CommandServiceTG(TelegramBotClient botClient, GameWorld world,
                            MovementService movementService, LocationService locationService,
                            MapService mapService, InventoryService inventoryService,
                            ILogger<CommandServiceTG> logger, MenuServiceTG menuService,BattleService battleService, ItemService itemService, TraderHandler traderHandler, DatabaseService databaseService)
        {
            _botClient = botClient;
            _world = world;
            _movementService = movementService;
            _locationService = locationService;
            _mapService = mapService;
            _inventoryService = inventoryService ?? new InventoryService(botClient, world, itemService);
            _logger = logger;
            _menuService = menuService;
            _battleService = battleService;
            _itemService = itemService;
            _traderHandler = traderHandler;
            _databaseService = databaseService;
        }
        private List<Position> GetAdjacentPositions(int x, int y)
        {
            return new List<Position>
            {
                new Position(x, y - 1),
                new Position(x, y + 1),
                new Position(x - 1, y),
                new Position(x + 1, y)
            };
        }
        public async Task HandleCommand(long chatId, Player player, string messageText)
        {
            _logger.LogDebug("Начало HandleCommand");
            try
            {
                _logger.LogDebug("HandleCommand: chatId={ChatId}, message={Message}", chatId, messageText);

                
                if (messageText == "🏠 Меню")
                {
                    await _menuService.ShowMainMenu(chatId);
                    return;
                }
                var command = messageText.ToLower();
                switch (command)
                {
                    case "/start":
                        await HandleStartCommand(chatId, player);
                        break;

                    case "⬆️ север":
                    case "север":
                    case "north":
                        await _movementService.ShowMovementAnimation(chatId, "север");
                        await _movementService.MovePlayer(player, "север");
                        break;
                    case "⬇️ юг":
                    case "юг":
                    case "south":
                        await _movementService.ShowMovementAnimation(chatId, "юг");
                        await _movementService.MovePlayer(player, "юг");
                        break;
                    case "⬅️ запад":
                    case "запад":
                    case "west":
                        await _movementService.ShowMovementAnimation(chatId, "запад");
                        await _movementService.MovePlayer(player, "запад");
                        break;
                    case "➡️ восток":
                    case "восток":
                    case "east":
                        await _movementService.ShowMovementAnimation(chatId, "восток");
                        await _movementService.MovePlayer(player, "восток");
                        break;

                    case "🗺️ карта мира":
                    case "карта мира":
                        await _mapService.ShowWorldMap(chatId, player);
                        break;
                    case "🗺️ карта":
                    case "карта":
                        await _mapService.ShowLocationMap(chatId, player);
                        break;

                    case "🔍 осмотреть":
                    case "осмотреть":
                        await HandleExamineCommand(chatId, player);
                        break;
                    case "💬 поговорить":
                    case "поговорить":
                        await HandleTalkCommand(chatId, player);
                        break;
                    case "⚔️ атаковать":
                    case "атаковать":
                        await HandleAttackCommand(chatId, player);
                        break;

                    case "🎒 инвентарь":
                    case "инвентарь":
                        await _inventoryService.ShowInteractiveInventory(chatId, player);
                        break;
                    case "📊 статус":
                    case "статус":
                        await ShowStatus(chatId, player);
                        break;
                    case "💪 навыки":
                    case "навыки":
                        await ShowAbilities(chatId, player);
                        break;
                    case "🎽 экипировка":
                    case "экипировка":
                        await ShowEquipmentMenu(chatId, player);
                        break;

                    case "⚙️ помощь":
                    case "помощь":
                        await ShowHelp(chatId);
                        break;
                    case "🏠 Меню":
                    case "меню":
                        await _menuService.ShowMainMenu(chatId);
                        break;
                    default:
                        await HandleUnknownCommand(chatId);
                        break;

                }
            }
            finally
            {
                _logger.LogDebug("HandleCommand: messageText={MessageText}", messageText);
            }
        }

        private async Task HandleStartCommand(long chatId, Player player)
        {
            var welcomeText = @"🎮 *Добро пожаловать в Metroidvania Bot!*

Теперь каждая локация - это огромная территория 10x10 для исследования!

*Новые возможности:*
• 🗺️ **Исследуйте локации** - перемещайтесь по сетке 10x10
• 📍 **Обнаруживайте объекты** - сундуки, NPC, враги
• 🧭 **Находите выходы** - переходы между локациями
• 🔍 **Исследуйте территорию** - открывайте новые области

*Управление:*
• Используйте кнопки движения для перемещения
• '🔍 Осмотреть' - исследовать текущую позицию
• '🗺️ Карта' - карта текущей локации
• '🗺️ Карта мира' - общая карта

*Удачи в исследовании Аркадии!*";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: welcomeText,
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardHelper.GetMovementKeyboard());

            await _locationService.DescribeLocation(chatId, player);
        }

        private async Task HandleExamineCommand(long chatId, Player player)
        {
            var location = _world.Locations[player.CurrentLocation];
            if (location.Objects.ContainsKey("chests"))
            {
                var chest = location.Objects["chests"].FirstOrDefault(c => c.X == player.PositionX && c.Y == player.PositionY);
                if (chest != null)
                {
                    location.Objects["chests"].Remove(chest);
                    var rng = new Random();
                    int goldReward = rng.Next(20, 50);
                    player.Gold += goldReward;
                    await _databaseService.SavePlayerAsync(player);
                    await _botClient.SendTextMessageAsync(chatId, $"🎁 Вы открыли сундук и нашли {goldReward}💰!");
                   
                    return;
                }
            }
        }
        public async Task ShowEquipmentMenu(long chatId, Player player)
        {
            var weapon = player.EquippedWeaponId != 0 ? _itemService.GetItemById(player.EquippedWeaponId) : null;
            var armor = player.EquippedArmorId != 0 ? _itemService.GetItemById(player.EquippedArmorId) : null;
            var text = $"🎽 *ЭКИПИРОВКА*\n\n" +
                       $"⚔️ Оружие: {(weapon != null ? weapon.Name : "нет")}\n" +
                       $"🛡️ Броня: {(armor != null ? armor.Name : "нет")}\n\n" +
                       $"Выберите действие:";
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⚔️ Экипировать оружие", "equip_weapon") },
                new[] { InlineKeyboardButton.WithCallbackData("🛡️ Экипировать броню", "equip_armor") },
                new[] { InlineKeyboardButton.WithCallbackData("🗑️ Снять оружие", "unequip_weapon") },
                new[] { InlineKeyboardButton.WithCallbackData("🗑️ Снять броню", "unequip_armor") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "back_to_game") }
            });
            await _botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }
        private async Task HandleTalkCommand(long chatId, Player player)
        {
            var location = _world.Locations[player.CurrentLocation];
            bool isTraderHere = (player.PositionX == 5 && player.PositionY == 2) ||
                                GetAdjacentPositions(player.PositionX, player.PositionY).Any(p => p.X == 5 && p.Y == 2);
            if (isTraderHere && location.Id == "start")
            {
                await _traderHandler.ShowTraderMenu(chatId, player);
                return;
            }

            var adjacent = GetAdjacentPositions(player.PositionX, player.PositionY);
            var hasNpc = adjacent.Any(pos => _locationService.GetObjectsAtPosition(location, pos.X, pos.Y).Any(o => o.Contains("NPC")))
                         || _locationService.GetObjectsAtPosition(location, player.PositionX, player.PositionY).Any(o => o.Contains("NPC"));
            if (hasNpc)
                await _botClient.SendTextMessageAsync(chatId, "💬 Вы заговорили с NPC, но он пока молчит...");
            else
                await _botClient.SendTextMessageAsync(chatId, "💬 Здесь не с кем поговорить.");
        }

        public async Task ShowTraderMenu(long chatId, Player player)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🛒 Купить предметы", "trader_items") },
                new[] { InlineKeyboardButton.WithCallbackData("✨ Купить способности", "trader_abilities") },
                new[] { InlineKeyboardButton.WithCallbackData("📜 Квесты", "trader_quests") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "back_to_game") }
            });
            await _botClient.SendTextMessageAsync(chatId, "🏪 *Торговец*\nЧем могу помочь?", parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        private async Task HandleAttackCommand(long chatId, Player player)
        {
            if (player.LocationMobs == null)
                player.LocationMobs = new Dictionary<string, List<MobInstance>>();

            var location = _world.Locations[player.CurrentLocation];
            if (!player.LocationMobs.TryGetValue(location.Id, out var mobs) || mobs == null || mobs.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "⚔️ Вокруг нет врагов.");
                return;
            }

            var adjacent = GetAdjacentPositions(player.PositionX, player.PositionY);
            var mob = adjacent.Select(pos => mobs.FirstOrDefault(m => m.X == pos.X && m.Y == pos.Y))
                             .FirstOrDefault(m => m != null);
            if (mob != null)
            {
                await _battleService.StartMobBattle(chatId, player, mob);
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "⚔️ Нет врагов рядом.");
            }
        }
        private async Task ShowStatus(long chatId, Player player)
        {
            try
            {
                var expForNextLevel = PlayerService.CalculateExpForNextLevel(player.Level);

                var itemsDisplay = string.Join(", ", player.Inventory.Select(name =>
                {
                    var item = _itemService.GetItemByName(name);
                    return item != null ? item.GetDisplayName() : name;
                }));

                var statusText = $@"📊 *СТАТУС ПЕРСОНАЖА*

*Имя:* {player.Name ?? "Не задано"}
*Раса:* {player.Race ?? "Не выбрана"}
*Класс:* {player.Class ?? "Не выбран"}
*Пол:* {(player.Gender == "Male" ? "👨 Мужской" : player.Gender == "Female" ? "👩 Женский" : "Не выбран")}

❤️ Здоровье: {player.Health.Current}/{player.Health.Max}
🔮 Мана: {player.Mana.Current}/{player.Mana.Current}
💪 Выносливость: {player.Stamina.Current}/{player.Stamina.Current}
🛡️ Защита: {player.Defense}
💪 Сила: {player.Strength}

💰 Золото: {player.Gold}

⭐ Уровень: {player.Level}
🎯 Опыт: {player.Experience}/{expForNextLevel}
📍 Локация: {_world.Locations[player.CurrentLocation].Name}

💪 *Способности:* {(player.AbilityNames.Count > 0 ? string.Join(", ", player.AbilityNames) : "Нет")}

🎒 *Предметы:* {(player.Inventory.Count > 0 ? itemsDisplay : "Пусто")}";

                if (!string.IsNullOrEmpty(player.IconPath))
                {
                    try
                    {
                        string iconFullPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", player.IconPath);
                        if (System.IO.File.Exists(iconFullPath))
                        {
                            using var stream = System.IO.File.OpenRead(iconFullPath);
                            await _botClient.SendPhotoAsync(
                                chatId: chatId,
                                photo: new InputOnlineFile(stream, "character_icon.jpg"),
                                caption: statusText,
                                parseMode: ParseMode.Markdown);
                            return;
                        }
                        else
                        {
                            _logger.LogWarning("Файл иконки не найден: {FilePath}", iconFullPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка загрузки иконки {FilePath}: {Message}", player.IconPath, ex.Message);
                    }
                }

                await _botClient.SendTextMessageAsync(chatId: chatId, text: statusText, parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка отображения статуса: {ex.Message}");
            }
        }

        private async Task ShowAbilities(long chatId, Player player)
        {
            var abilitiesText = "💪 *СПОСОБНОСТИ*\n\n";

            if (player.AbilityNames.Count > 0)
            {
                foreach (var ability in player.AbilityNames)
                    abilitiesText += $"• {ability}\n";
            }
            else
            {
                abilitiesText += "🚫 У вас пока нет способностей.\nИсследуйте мир, чтобы найти новые силы!";
            }

            await _botClient.SendTextMessageAsync(chatId: chatId, text: abilitiesText, parseMode: ParseMode.Markdown);
        }

        private async Task ShowHelp(long chatId)
        {
            var helpText = @"⚙️ *ПОМОЩЬ*

*Основные команды:*
• ⬆️ Север / ⬇️ Юг / ⬅️ Запад / ➡️ Восток - Перемещение
• 🗺️ Карта - Интерактивная карта мира
• 🎒 Инвентарь - Управление предметами
• 📊 Статус - Информация о персонаже
• 💪 Навыки - Список способностей
• 🔍 Осмотреть - Детальный осмотр локации

*Геймплей:*
• Исследуйте локации для поиска предметов
• Находите новые способности для доступа к новым зонам
• Собирайте ключи и артефакты
• Сражайтесь с боссами

*Управление:* Используйте кнопки или вводите команды текстом.";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: helpText,
                parseMode: ParseMode.Markdown,
                replyMarkup: KeyboardHelper.GetEnhancedControls());
        }

        private async Task HandleUnknownCommand(long chatId)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Неизвестная команда. Используйте кнопки или введите /help для справки.",
                replyMarkup: KeyboardHelper.GetMovementKeyboard());
        }

    }
}