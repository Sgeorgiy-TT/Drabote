using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Gameplay;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Gameplay.Location;

namespace TelegramCasinoBot.Services.Models.Gameplay
{
    public class MovementService
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly LocationService _locationService;
        private readonly ILogger<MovementService> _logger;
        private readonly MobSpawnService _mobSpawnService;
        private readonly BattleService _battleService;
        private readonly DatabaseService _databaseService; 

        public MovementService(
            TelegramBotClient botClient,
            GameWorld world,
            LocationService locationService,
            ILogger<MovementService> logger,
            MobSpawnService mobSpawnService,
            BattleService battleService,
            DatabaseService databaseService)
        {
            _botClient = botClient;
            _world = world;
            _locationService = locationService;
            _logger = logger ?? NullLogger<MovementService>.Instance;
            _mobSpawnService = mobSpawnService ?? throw new ArgumentNullException(nameof(mobSpawnService));
            _battleService = battleService ?? throw new ArgumentNullException(nameof(battleService));
            _databaseService = databaseService;
        }

        public async Task<bool> MovePlayer(Player player, string direction)
        {
            _logger.LogDebug("MovePlayer: direction={Direction}, player={PlayerName}, speed={Speed}",
                direction, player.Name ?? "Unknown", player.SpeedBoost);

            var currentLocation = _world.Locations[player.CurrentLocation];
            int dx = 0, dy = 0;

            switch (direction.ToLower())
            {
                case "север": case "north": dy = -1; break;
                case "юг": case "south": dy = 1; break;
                case "запад": case "west": dx = -1; break;
                case "восток": case "east": dx = 1; break;
                default: return false;
            }

            int newX = player.PositionX;
            int newY = player.PositionY;
            int steps = player.SpeedBoost;
            bool moved = false;

            for (int step = 1; step <= steps; step++)
            {
                int targetX = player.PositionX + dx * step;
                int targetY = player.PositionY + dy * step;

                if (targetX < 0 || targetX >= currentLocation.Width || targetY < 0 || targetY >= currentLocation.Height)
                {
                    await _botClient.SendTextMessageAsync(player.ChatId, "🚫 Дальше пути нет! Это край локации.");
                    break;
                }

                var exit = CheckForLocationExit(currentLocation, targetX, targetY);
                if (exit != null)
                {
                    player.PositionX = targetX;
                    player.PositionY = targetY;
                    AddToExploredAreas(player, targetX, targetY);
                    return await HandleLocationTransition(player, exit);
                }

                if (CheckForObstacles(currentLocation, targetX, targetY))
                {
                    await _botClient.SendTextMessageAsync(player.ChatId, "🚫 На пути препятствие! Дальше двигаться нельзя.");
                    break;
                }

                newX = targetX;
                newY = targetY;
                moved = true;
            }

            if (!moved) return false;

            player.PositionX = newX;
            player.PositionY = newY;
            AddToExploredAreas(player, newX, newY);

            if (!player.LocationMobs.ContainsKey(player.CurrentLocation))
            {
                var newMobs = await _mobSpawnService.GenerateInitialMobs(currentLocation, player.PositionX, player.PositionY);
                player.LocationMobs[player.CurrentLocation] = newMobs;
            }
            else
            {
                await _mobSpawnService.RespawnMobsIfNeeded(currentLocation, player.LocationMobs[player.CurrentLocation], player.PositionX, player.PositionY);
            }

            var mobHere = player.LocationMobs[player.CurrentLocation].FirstOrDefault(m => m.X == newX && m.Y == newY);
            if (mobHere != null)
            {
                await _battleService.StartMobBattle(player.ChatId, player, mobHere);
                return true;
            }

            await _locationService.DescribeLocation(player.ChatId, player);
            await _databaseService.SavePlayerAsync(player);
            return true;
        }

        private LocationExit CheckForLocationExit(GameLocation location, int x, int y)
        {
            foreach (var exit in location.Exits)
            {
                if (exit.Position.X == x && exit.Position.Y == y)
                {
                    return exit;
                }
            }
            return null;
        }

        private async Task<bool> HandleLocationTransition(Player player, LocationExit exit)
        {
            var targetLocation = _world.Locations[exit.TargetLocationId];

            if (!string.IsNullOrEmpty(targetLocation.RequiredAbility) &&
                !player.AbilityNames.Contains(targetLocation.RequiredAbility))
            {
                _logger.LogDebug("Player {PlayerName} lacks required ability {Ability} for location {Location}",
                    player.Name ?? "Unknown", targetLocation.RequiredAbility, targetLocation.Id);
                await _botClient.SendTextMessageAsync(player.ChatId,
                    targetLocation.AccessDeniedMessage ?? $"🚫 Нужна способность: {targetLocation.RequiredAbility}");
                return false;
            }

            var newPosition = CalculateEntryPosition(exit.Direction, targetLocation);

            _logger.LogInformation("Player {PlayerName} transitioning from {From} to {To}",
                player.Name ?? "Unknown", player.CurrentLocation, exit.TargetLocationId);

            player.CurrentLocation = exit.TargetLocationId;
            player.PositionX = newPosition.X;
            player.PositionY = newPosition.Y;
            if (player.CurrentLocation == "boss_chamber" && player.BossHealth <= 0)
            {
                await _battleService.StartBossBattle(player.ChatId, player);
                return true;
            }
            AddToExploredAreas(player, newPosition.X, newPosition.Y);

            await _botClient.SendTextMessageAsync(player.ChatId,
                $"🚪 {exit.Description ?? "Вы переходите в новую локацию..."}");

            await _locationService.DescribeLocation(player.ChatId, player);
            return true;
        }

        private Position CalculateEntryPosition(string direction, GameLocation targetLocation)
        {
            return direction.ToLower() switch
            {
                "north" => new Position(targetLocation.Width / 2, targetLocation.Height - 2),
                "south" => new Position(targetLocation.Width / 2, 1),
                "east" => new Position(1, targetLocation.Height / 2),
                "west" => new Position(targetLocation.Width - 2, targetLocation.Height / 2),
                _ => new Position(targetLocation.Width / 2, targetLocation.Height / 2)
            };
        }

        private bool CheckForObstacles(GameLocation location, int x, int y)
        {
            if (location.Objects.ContainsKey("obstacles"))
            {
                foreach (var obstacle in location.Objects["obstacles"])
                {
                    if (obstacle.X == x && obstacle.Y == y)
                        return true;
                }
            }
            return false;
        }

        private void AddToExploredAreas(Player player, int x, int y)
        {
            var locationId = player.CurrentLocation;
            if (!player.ExploredAreas.ContainsKey(locationId))
            {
                player.ExploredAreas[locationId] = new List<Position>();
            }

            var pos = new Position(x, y);
            if (!player.ExploredAreas[locationId].Exists(p => p.X == x && p.Y == y))
            {
                player.ExploredAreas[locationId].Add(pos);
            }
        }

        public async Task ShowMovementAnimation(long chatId, string direction)
        {
            _logger.LogDebug("Начало ShowMovementAnimation для chatId {ChatId}, direction {Direction}", chatId, direction);
            try
            {
                string animationSymbol = direction.ToLower() switch
                {
                    "север" or "north" => "⬆️",
                    "юг" or "south" => "⬇️",
                    "запад" or "west" => "⬅️",
                    "восток" or "east" => "➡️",
                    _ => "🎯"
                };

                var animationMessage = await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"{animationSymbol} Перемещение...");

                await Task.Delay(800);
                await _botClient.DeleteMessageAsync(chatId, animationMessage.MessageId);
            }
            finally
            {
                _logger.LogDebug("ShowMovementAnimation завершён для chatId {ChatId}", chatId);
            }
        }

    }
}