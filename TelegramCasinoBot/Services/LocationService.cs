using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InputFiles;
using System.IO;

namespace TelegramMetroidvaniaBot.Services
{
    public class LocationService
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly MapGeneratorService _mapGenerator;
        private readonly LoggerService _logger = LoggerService.Instance;

        public LocationService(TelegramBotClient botClient, GameWorld world)
        {
            _botClient = botClient;
            _world = world;
            _mapGenerator = new MapGeneratorService();
        }

        public async Task DescribeLocation(long chatId, Player player)
        {
            if (!_world.Locations.ContainsKey(player.CurrentLocation))
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Локация не найдена!");
                return;
            }

            var location = _world.Locations[player.CurrentLocation];

            try
            {
                // Генерируем карту с визуализацией
                await SendLocationWithVisualMap(chatId, player, location);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Ошибка отправки визуальной карты: {ex.Message}");
                await SendTextLocationDescription(chatId, player, location);
            }

            
        }

        private async Task SendLocationWithVisualMap(long chatId, Player player, Location location)
        {
            try
            {
                if (!System.IO.File.Exists(location.ImagePath))
                {
                    await SendTextLocationDescription(chatId, player, location);
                    return;
                }

                var exploredAreas = player.ExploredAreas.ContainsKey(location.Id)
                    ? player.ExploredAreas[location.Id]
                    : new List<Position>();

                var allObjects = new Dictionary<string, List<Position>>();
                foreach (var obj in location.Objects)
                {
                    allObjects[obj.Key] = new List<Position>(obj.Value);
                }

                // ВАЖНО: порядок аргументов должен совпадать с сигнатурой в MapGeneratorService
                // Предполагаемая сигнатура: (string baseImagePath, int playerX, int playerY, int gridWidth, int gridHeight,
                //                             List<Position> exploredAreas, Dictionary<string, List<Position>> locationObjects,
                //                             List<LocationExit> exits, string playerSpritePath)
                using var mapStream = await _mapGenerator.GenerateLocationMap(
                    location.ImagePath,
                    player.PositionX,
                    player.PositionY,
                    location.Width,
                    location.Height,
                    exploredAreas,
                    allObjects,
                    location.Exits,
                    player.IconPath
                );

                var positionInfo = GeneratePositionInfo(player, location);
                var caption = $"*{location.Name}*\n\n{location.Description}\n\n{positionInfo}";

                await _botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: new InputOnlineFile(mapStream, "location_map.png"),
                    caption: caption,
                    parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка генерации визуальной карты: {ex.Message}", ex);
                await SendTextLocationDescription(chatId, player, location);
            }
        }

        private string GeneratePositionInfo(Player player, Location location)
        {
            var info = $"📍 *Позиция: [{player.PositionX},{player.PositionY}]*\n";

            // Добавляем информацию об исследовании
            var explorationProgress = GetExplorationProgress(player, location);
            info += $"\n🔍 Исследовано: {explorationProgress}%";

            return info;
        }

        private double GetExplorationProgress(Player player, Location location)
        {
            if (!player.ExploredAreas.ContainsKey(location.Id))
                return 0;

            var totalCells = location.Width * location.Height;
            var exploredCells = player.ExploredAreas[location.Id].Count;

            return Math.Round((double)exploredCells / totalCells * 100, 1);
        }

        private bool IsObstacle(Location location, int x, int y)
        {
            if (location.Objects.ContainsKey("obstacles"))
            {
                return location.Objects["obstacles"].Exists(pos => pos.X == x && pos.Y == y);
            }
            return false;
        }

        // Остальные методы остаются без изменений...
        private async Task SendTextLocationDescription(long chatId, Player player, Location location)
        {
            var grid = GenerateTextGrid(player, location);
            var message = $"*{location.Name}*\n\n{location.Description}\n\n```\n{grid}\n```\n📍 *Позиция: [{player.PositionX},{player.PositionY}]*";

            await _botClient.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Markdown);
        }

        private string GenerateTextGrid(Player player, Location location)
        {
            var grid = "";
            for (int y = 0; y < location.Height; y++)
            {
                for (int x = 0; x < location.Width; x++)
                {
                    if (x == player.PositionX && y == player.PositionY)
                    {
                        grid += "👤 "; // Игрок
                    }
                    else if (IsExplored(player, x, y))
                    {
                        var obj = GetObjectSymbol(location, x, y);
                        grid += obj + " ";
                    }
                    else
                    {
                        grid += "▪️ "; // Неисследовано
                    }
                }
                grid += "\n";
            }
            return grid;
        }

        private string GetObjectSymbol(Location location, int x, int y)
        {
            foreach (var objType in location.Objects)
            {
                foreach (var pos in objType.Value)
                {
                    if (pos.X == x && pos.Y == y)
                    {
                        return objType.Key switch
                        {
                            "chests" => "📦",
                            "npcs" => "🧝",
                            "enemies" => "👹",
                            "obstacles" => "🚫",
                            "exits" => "🚪",
                            _ => "●"
                        };
                    }
                }
            }
            return "·"; // Пустая клетка
        }

        public List<string> GetObjectsAtPosition(Location location, int x, int y)
        {
            var objects = new List<string>();

            foreach (var objType in location.Objects)
            {
                foreach (var pos in objType.Value)
                {
                    if (pos.X == x && pos.Y == y)
                    {
                        objects.Add(objType.Key switch
                        {
                            "chests" => "📦 Сундук",
                            "npcs" => "🧝 NPC",
                            "enemies" => "👹 Враг",
                            "exits" => "🚪 Выход",
                            _ => "Объект"
                        });
                    }
                }
            }
            return objects;
        }

        private async Task DescribePosition(long chatId, Player player)
        {
            var location = _world.Locations[player.CurrentLocation];

            // Проверяем объекты в текущей позиции
            var objectsHere = GetObjectsAtPosition(location, player.PositionX, player.PositionY);

            var message = $"📍 *Позиция: [{player.PositionX},{player.PositionY}]*\n";

            if (objectsHere.Any())
            {
                message += "\n📋 *Здесь есть:*\n" + string.Join("\n", objectsHere);
            }

            // Показываем доступные направления
            message += $"\n{GetAvailableDirections(player)}";

            var keyboard = GetMovementKeyboard();
            await _botClient.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        private string GetAvailableDirections(Player player)
        {
            var directions = new List<string>();
            var location = _world.Locations[player.CurrentLocation];

            if (player.PositionY > 0) directions.Add("⬆️ Север");
            if (player.PositionY < location.Height - 1) directions.Add("⬇️ Юг");
            if (player.PositionX > 0) directions.Add("⬅️ Запад");
            if (player.PositionX < location.Width - 1) directions.Add("➡️ Восток");

            return "🧭 Направления: " + string.Join(" • ", directions);
        }

        private bool IsExplored(Player player, int x, int y)
        {
            return player.ExploredAreas.ContainsKey(player.CurrentLocation) &&
                   player.ExploredAreas[player.CurrentLocation].Exists(p => p.X == x && p.Y == y);
        }

        private ReplyKeyboardMarkup GetMovementKeyboard() => KeyboardHelper.GetMovementKeyboard();

        public async Task HandleLocationEvents(long chatId, Player player)
        {
            var location = _world.Locations[player.CurrentLocation];

            switch (location.Id)
            {
                case "ancient_temple" when !player.Abilities.Contains("Двойной прыжок"):
                    await ShowAbilityUnlockAnimation(chatId, "Двойной прыжок", "💫");
                    player.Abilities.Add("Двойной прыжок");
                    break;

                case "crystal_cave" when !player.Abilities.Contains("Лазерный луч"):
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] {
                            InlineKeyboardButton.WithCallbackData("🔮 Изучить кристалл", "learn_laser"),
                            InlineKeyboardButton.WithCallbackData("💥 Атаковать кристалл", "attack_crystal")
                        }
                    });

                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "🔮 *Загадочный кристалл* излучает мощную энергию...",
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard);
                    break;
            }
        }

        public async Task ShowAbilityUnlockAnimation(long chatId, string ability, string emoji)
        {
            var messages = new[]
            {
                $"{emoji} Обнаружена новая сила...",
                $"{emoji} {ability}...",
                $"🎉 *{ability}* разблокирован!"
            };

            foreach (var msg in messages)
            {
                var sentMsg = await _botClient.SendTextMessageAsync(chatId, msg,
                    parseMode: ParseMode.Markdown);
                await Task.Delay(1200);
                await _botClient.DeleteMessageAsync(chatId, sentMsg.MessageId);
            }
        }
    }
}