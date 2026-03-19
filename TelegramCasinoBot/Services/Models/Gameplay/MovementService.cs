using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramMetroidvaniaBot;
using TelegramMetroidvaniaBot.Models;

namespace TelegramCasinoBot.Services.Models.Gameplay
{
    public class MovementService
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly LocationService _locationService;
        private readonly ILogger<MovementService> _logger;

        public MovementService(TelegramBotClient botClient, GameWorld world, LocationService locationService, ILogger<MovementService> logger = null)
        {
            _botClient = botClient;
            _world = world;
            _locationService = locationService;
            _logger = logger ?? NullLogger<MovementService>.Instance;
        }

        public async Task<bool> MovePlayer(Player player, string direction)
        {
            _logger.LogDebug("Начало MovePlayer: direction={Direction}, player={PlayerName}", direction, player.Name ?? "Unknown");
            try
            {
                _logger.LogDebug("MovePlayer called: direction={Direction}, player={PlayerName}, location={Location}",
                    direction, player.Name ?? "Unknown", player.CurrentLocation);

                var currentLocation = _world.Locations[player.CurrentLocation];
                int newX = player.PositionX;
                int newY = player.PositionY;

                switch (direction.ToLower())
                {
                    case "север": case "north": newY--; break;
                    case "юг": case "south": newY++; break;
                    case "запад": case "west": newX--; break;
                    case "восток": case "east": newX++; break;
                }

                if (newX < 0 || newX >= currentLocation.Width || newY < 0 || newY >= currentLocation.Height)
                {
                    _logger.LogDebug("Player hit boundary at ({X}, {Y})", newX, newY);
                    await _botClient.SendTextMessageAsync(player.ChatId, "🚫 Дальше пути нет! Это край локации.");
                    return false;
                }

                var exit = CheckForLocationExit(currentLocation, newX, newY);
                if (exit != null)
                {
                    _logger.LogDebug("Player found exit to {TargetLocation}", exit.TargetLocationId);
                    return await HandleLocationTransition(player, exit);
                }

                if (CheckForObstacles(currentLocation, newX, newY))
                {
                    _logger.LogDebug("Player hit obstacle at ({X}, {Y})", newX, newY);
                    await _botClient.SendTextMessageAsync(player.ChatId, "🚫 Здесь невозможно пройти! На пути препятствие.");
                    return false;
                }

                player.PositionX = newX;
                player.PositionY = newY;

                _logger.LogDebug("Player moved to position ({X}, {Y})", newX, newY);

                AddToExploredAreas(player, newX, newY);

                await _locationService.DescribeLocation(player.ChatId, player);
                return true;
            }
            finally
            {
                _logger.LogDebug("MovePlayer завершён для {PlayerName}", player.Name ?? "Unknown");
            }
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
                !player.Abilities.Contains(targetLocation.RequiredAbility))
            {
                _logger.LogWarning("Player {PlayerName} lacks required ability {Ability} for location {Location}",
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