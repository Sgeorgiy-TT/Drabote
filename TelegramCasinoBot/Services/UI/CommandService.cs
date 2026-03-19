using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.DataStats;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Utils;
using TelegramMetroidvaniaBot;

namespace TelegramCasinoBot.Services.UI
{
    public class CommandService
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly MovementService _movementService;
        private readonly LocationService _locationService;
        private readonly MapService _mapService;
        private readonly InventoryService _inventoryService;
        private readonly ILogger<CommandService> _logger;

        public CommandService(TelegramBotClient botClient, GameWorld world,
                            MovementService movementService, LocationService locationService,
                            MapService mapService, InventoryService inventoryService,
                            ILogger<CommandService> logger)
        {
            _botClient = botClient;
            _world = world;
            _movementService = movementService;
            _locationService = locationService;
            _mapService = mapService;
            _inventoryService = inventoryService ?? new InventoryService(botClient, world);
            _logger = logger;
        }

        public async Task HandleCommand(long chatId, Player player, string messageText)
        {
            _logger.LogDebug("Начало HandleCommand");
            try
            {
                _logger.LogDebug("HandleCommand: chatId={ChatId}, message={Message}", chatId, messageText);

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

                    case "⚙️ помощь":
                    case "помощь":
                        await ShowHelp(chatId);
                        break;

                    default:
                        await HandleUnknownCommand(chatId);
                        break;
                }
            }
            finally
            {
                _logger.LogDebug("HandleCommand завершён");
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
            var objectsHere = _locationService.GetObjectsAtPosition(location, player.PositionX, player.PositionY);

            if (objectsHere.Count > 0)
            {
                var message = "🔍 *Осмотр местности:*\n\n";
                foreach (var obj in objectsHere)
                    message += $"• {obj}\n";
                await _botClient.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Markdown);
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "🔍 Здесь нет ничего интересного.");
            }
        }

        private async Task HandleTalkCommand(long chatId, Player player)
        {
            var location = _world.Locations[player.CurrentLocation];
            var objectsHere = _locationService.GetObjectsAtPosition(location, player.PositionX, player.PositionY);
            bool hasNpc = objectsHere.Exists(o => o.Contains("NPC"));

            if (hasNpc)
                await _botClient.SendTextMessageAsync(chatId, "💬 Вы пытаетесь заговорить, но NPC пока не отвечают...");
            else
                await _botClient.SendTextMessageAsync(chatId, "💬 Здесь не с кем поговорить.");
        }

        private async Task HandleAttackCommand(long chatId, Player player)
        {
            var location = _world.Locations[player.CurrentLocation];
            var objectsHere = _locationService.GetObjectsAtPosition(location, player.PositionX, player.PositionY);
            bool hasEnemy = objectsHere.Exists(o => o.Contains("Враг"));

            if (hasEnemy)
                await _botClient.SendTextMessageAsync(chatId, "⚔️ Вы готовы к бою! Система сражений в разработке...");
            else
                await _botClient.SendTextMessageAsync(chatId, "⚔️ Здесь нет врагов для атаки.");
        }

        private async Task ShowStatus(long chatId, Player player)
        {
            try
            {
                var expForNextLevel = PlayerService.CalculateExpForNextLevel(player.Level);

                var statusText = $@"📊 *СТАТУС ПЕРСОНАЖА*

*Имя:* {player.Name ?? "Не задано"}
*Раса:* {player.Race ?? "Не выбрана"}
*Класс:* {player.Class ?? "Не выбран"}
*Пол:* {(player.Gender == "Male" ? "👨 Мужской" : player.Gender == "Female" ? "👩 Женский" : "Не выбран")}

❤️ Здоровье: {player.Health}/{player.MaxHealth}
🔮 Мана: {player.Mana}/{player.MaxMana}
💪 Выносливость: {player.Stamina}/{player.MaxStamina}
🛡️ Защита: {player.Defense}

⭐ Уровень: {player.Level}
🎯 Опыт: {player.Experience}/{expForNextLevel}
📍 Локация: {_world.Locations[player.CurrentLocation].Name}

💪 *Способности:* {(player.Abilities.Count > 0 ? string.Join(", ", player.Abilities) : "Нет")}
🎒 *Предметы:* {(player.Inventory.Count > 0 ? string.Join(", ", player.Inventory) : "Пусто")}";

                if (!string.IsNullOrEmpty(player.IconPath))
                {
                    try
                    {
                        string iconFullPath = Path.Combine(Directory.GetCurrentDirectory(), player.IconPath);
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

            if (player.Abilities.Count > 0)
            {
                foreach (var ability in player.Abilities)
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