using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMetroidvaniaBot.Services
{
    public class MapService
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly ILogger<MapService> _logger;

        public MapService(TelegramBotClient botClient, GameWorld world, ILogger<MapService> logger = null)
        {
            _botClient = botClient;
            _world = world;
            _logger = logger ?? NullLogger<MapService>.Instance;
        }

        public async Task ShowWorldMap(long chatId, Player player)
        {
            _logger.LogDebug("ShowWorldMap called for chatId: {ChatId}", chatId);

            var map = GenerateWorldMap(player);
            var legend = GetWorldMapLegend();

            var message = $"🗺️ *КАРТА МИРА АРКАДИИ*\n\n```\n{map}\n```\n{legend}";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔄 Обновить", "refresh_world_map"),
                    InlineKeyboardButton.WithCallbackData("📍 Текущая локация", "show_current_location")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📊 Статистика", "map_stats"),
                    InlineKeyboardButton.WithCallbackData("🔍 Детали", "map_details")
                }
            });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }
        public async Task ShowInteractiveMap(long chatId, Player player)
        {
            var location = _world.Locations[player.CurrentLocation];
            var map = GenerateLocationMap(player, location);

            var message = $"🗺️ *КАРТА: {location.Name}*\n\n```\n{map}\n```\n*Ваша позиция: [{player.PositionX},{player.PositionY}]*";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄 Обновить карту", "refresh_map"),
            InlineKeyboardButton.WithCallbackData("📍 Показать локацию", "show_location")
        }
    });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }
        private string GenerateWorldMap(Player player)
        {
            var minX = _world.Locations.Values.Min(l => l.WorldMapX);
            var maxX = _world.Locations.Values.Max(l => l.WorldMapX);
            var minY = _world.Locations.Values.Min(l => l.WorldMapY);
            var maxY = _world.Locations.Values.Max(l => l.WorldMapY);

            var width = maxX - minX + 3;
            var height = maxY - minY + 3;

            string[,] grid = new string[height, width];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    grid[y, x] = " ";

            foreach (var location in _world.Locations.Values)
            {
                var mapX = location.WorldMapX - minX + 1;
                var mapY = location.WorldMapY - minY + 1;

                if (mapX >= 0 && mapX < width && mapY >= 0 && mapY < height)
                {
                    grid[mapY, mapX] = GetWorldMapSymbol(location.Id, player.CurrentLocation == location.Id);
                }
            }

            foreach (var location in _world.Locations.Values)
            {
                var x1 = location.WorldMapX - minX + 1;
                var y1 = location.WorldMapY - minY + 1;

                if (location.NorthLocation != null)
                {
                    var x2 = location.NorthLocation.WorldMapX - minX + 1;
                    var y2 = location.NorthLocation.WorldMapY - minY + 1;
                    DrawConnection(grid, x1, y1, x2, y2);
                }
                if (location.EastLocation != null)
                {
                    var x2 = location.EastLocation.WorldMapX - minX + 1;
                    var y2 = location.EastLocation.WorldMapY - minY + 1;
                    DrawConnection(grid, x1, y1, x2, y2);
                }
            }
            string mapString = "┌" + new string('─', width * 2 - 1) + "┐\n";
            for (int y = 0; y < height; y++)
            {
                mapString += "│";
                for (int x = 0; x < width; x++)
                {
                    mapString += grid[y, x] + " ";
                }
                mapString += "│\n";
            }
            mapString += "└" + new string('─', width * 2 - 1) + "┘";

            return mapString;
        }

        private string GetWorldMapSymbol(string locationId, bool isCurrent)
        {
            if (isCurrent) return "★";

            return locationId switch
            {
                "start" => "S",
                "ancient_temple" => "T",
                "crystal_cave" => "C",
                "forbidden_forest" => "F",
                "boss_chamber" => "B",
                "final_sanctum" => "W",
                _ => "?"
            };
        }

        private void DrawConnection(string[,] grid, int x1, int y1, int x2, int y2)
        {
            if (x1 == x2)
            {
                for (int y = Math.Min(y1, y2) + 1; y < Math.Max(y1, y2); y++)
                {
                    if (grid[y, x1] == " ") grid[y, x1] = "│";
                }
            }
            else if (y1 == y2)
            {
                for (int x = Math.Min(x1, x2) + 1; x < Math.Max(x1, x2); x++)
                {
                    if (grid[y1, x] == " ") grid[y1, x] = "─";
                }
            }
        }

        private string GetWorldMapLegend()
        {
            return @"*Легенда карты:*
★ - Ваше местоположение
S - Забытые Руины
T - Древний Храм  
C - Кристальная Пещера
F - Запретный Лес
B - Зал Стражей
W - Святилище Древних
│ ─ - Дороги и тропы

*Статистика:*
• Исследовано локаций: X/6
• Открыто областей: Y%
• Найдено секретов: Z";
        }

        public async Task ShowLocationMap(long chatId, Player player)
        {
            var location = _world.Locations[player.CurrentLocation];
            var map = GenerateLocationMap(player, location);

            var message = $"🗺️ *КАРТА: {location.Name}*\n\n```\n{map}\n```\n*Ваша позиция: [{player.PositionX},{player.PositionY}]*";

            await _botClient.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Markdown);
        }

        private string GenerateLocationMap(Player player, Location location)
        {
            var grid = "";
            for (int y = 0; y < location.Height; y++)
            {
                for (int x = 0; x < location.Width; x++)
                {
                    if (x == player.PositionX && y == player.PositionY)
                    {
                        grid += "👤";
                    }
                    else if (player.ExploredAreas.ContainsKey(location.Id) &&
                             player.ExploredAreas[location.Id].Exists(p => p.X == x && p.Y == y))
                    {
                        grid += GetExploredCellSymbol(location, x, y);
                    }
                    else
                    {
                        grid += "??";
                    }
                    grid += " ";
                }
                grid += "\n";
            }
            return grid;
        }

        private string GetExploredCellSymbol(Location location, int x, int y)
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
                            "exits" => "🚪",
                            "obstacles" => "█",
                            _ => "· "
                        };
                    }
                }
            }
            return "· ";
        }
    }
}