using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMetroidvaniaBot.Services
{
    public class LocationService
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly MapGeneratorService _mapGenerator;
        private readonly ILogger<LocationService> _logger;

        public LocationService(
            TelegramBotClient botClient,
            GameWorld world,
            MapGeneratorService mapGenerator,
            ILogger<LocationService> logger = null)
        {
            _botClient = botClient;
            _world = world;
            _mapGenerator = mapGenerator ?? throw new ArgumentNullException(nameof(mapGenerator));
            _logger = logger ?? NullLogger<LocationService>.Instance;
        }

        public async Task DescribeLocation(long chatId, Player player)
        {
            _logger.LogDebug("Начало DescribeLocation для chatId {ChatId}", chatId);
            try
            {
                if (!_world.Locations.ContainsKey(player.CurrentLocation))
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Локация не найдена!");
                    return;
                }

                var location = _world.Locations[player.CurrentLocation];

                try
                {
                    await SendLocationWithVisualMap(chatId, player, location);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка отправки визуальной карты: {Message}", ex.Message);
                    await SendTextLocationDescription(chatId, player, location);
                }
            }
            finally
            {
                _logger.LogDebug("DescribeLocation завершён для chatId {ChatId}", chatId);
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

                _logger.LogInformation("Размер сгенерированной карты: {Size} байт", mapStream.Length);

                int maxRetries = 3;
                int delayMs = 2000;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        mapStream.Position = 0;
                        await _botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: new InputOnlineFile(mapStream, "location_map.png"),
                            caption: caption,
                            parseMode: ParseMode.Markdown
                        );
                        _logger.LogDebug("Карта успешно отправлена с {Attempt}-й попытки", attempt);
                        return;
                    }
                    catch (Exception ex) when (attempt < maxRetries)
                    {
                        _logger.LogWarning(ex, "Попытка {Attempt} отправки карты не удалась, повтор через {Delay} мс...", attempt, delayMs);
                        await Task.Delay(delayMs);
                    }
                }
                throw new Exception($"Не удалось отправить карту после {maxRetries} попыток.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации/отправки визуальной карты: {Message}", ex.Message);
                await SendTextLocationDescription(chatId, player, location);
            }
        }

        private string GeneratePositionInfo(Player player, Location location)
        {
            var info = $"📍 *Позиция: [{player.PositionX},{player.PositionY}]*\n";
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
                        grid += "👤 ";
                    }
                    else if (IsExplored(player, x, y))
                    {
                        var obj = GetObjectSymbol(location, x, y);
                        grid += obj + " ";
                    }
                    else
                    {
                        grid += "▪️ ";
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
            return "·";
        }

        public List<string> GetObjectsAtPosition(Location location, int x, int y)
        {
            _logger.LogDebug("Начало GetObjectsAtPosition");
            try
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
            finally
            {
                _logger.LogDebug("GetObjectsAtPosition завершён");
            }
        }

        private bool IsExplored(Player player, int x, int y)
        {
            return player.ExploredAreas.ContainsKey(player.CurrentLocation) &&
                   player.ExploredAreas[player.CurrentLocation].Exists(p => p.X == x && p.Y == y);
        }

        private ReplyKeyboardMarkup GetMovementKeyboard() => KeyboardHelper.GetMovementKeyboard();

        public async Task HandleLocationEvents(long chatId, Player player)
        {
            _logger.LogDebug("Начало HandleLocationEvents для chatId {ChatId}", chatId);
            try
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
            finally
            {
                _logger.LogDebug("HandleLocationEvents завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task ShowAbilityUnlockAnimation(long chatId, string ability, string emoji)
        {
            _logger.LogDebug("Начало ShowAbilityUnlockAnimation для chatId {ChatId}", chatId);
            try
            {
                var messages = new[]
                {
                    $"{emoji} Обнаружена новая сила...",
                    $"{emoji} {ability}...",
                    $"🎉 *{ability}* разблокирован!"
                };

                foreach (var msg in messages)
                {
                    var sentMsg = await _botClient.SendTextMessageAsync(chatId, msg, parseMode: ParseMode.Markdown);
                    await Task.Delay(1200);
                    await _botClient.DeleteMessageAsync(chatId, sentMsg.MessageId);
                }
            }
            finally
            {
                _logger.LogDebug("ShowAbilityUnlockAnimation завершён для chatId {ChatId}", chatId);
            }
        }
    }
}