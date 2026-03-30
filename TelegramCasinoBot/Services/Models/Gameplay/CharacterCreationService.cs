using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Models.Stats;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Infrastructure;

namespace TelegramCasinoBot.Services.Models.Gameplay
{
    public class CharacterCreationService
    {
        private readonly DatabaseService _databaseService;
        private readonly PlayerManager _playerManager;
        private readonly ILogger<CharacterCreationService> _logger;
        private readonly Dictionary<long, Player> _characterCreationProgress = new();

        public CharacterCreationService(
            DatabaseService databaseService,
            PlayerManager playerManager,
            ILogger<CharacterCreationService> logger = null)
        {
            _databaseService = databaseService;
            _playerManager = playerManager;
            _logger = logger ?? NullLogger<CharacterCreationService>.Instance;
        }

        public void StartCharacterCreation(long chatId)
        {
            _logger.LogDebug("Начало создания персонажа для {ChatId}", chatId);
            _characterCreationProgress[chatId] = new Player(chatId);
        }

        public bool IsInCharacterCreation(long chatId) => _characterCreationProgress.ContainsKey(chatId);

        public Player GetCharacterInProgress(long chatId) => _characterCreationProgress.GetValueOrDefault(chatId);

        public void SetName(long chatId, string name)
        {
            if (_characterCreationProgress.TryGetValue(chatId, out var player))
                player.Name = name;
        }

        public void SetGender(long chatId, string gender)
        {
            if (_characterCreationProgress.TryGetValue(chatId, out var player))
                player.Gender = gender;
        }

        public void SetIconPath(long chatId, string iconPath)
        {
            if (_characterCreationProgress.TryGetValue(chatId, out var player))
                player.IconPath = iconPath;
        }

        public async Task<bool> ApplyRace(long chatId, Race race)
        {
            if (!_characterCreationProgress.TryGetValue(chatId, out var player))
                return false;
            player.ApplyRace(race);
            player.RecalculateStats();
            return true;
        }

        public async Task<bool> ApplyClass(long chatId, Class playerClass)
        {
            if (!_characterCreationProgress.TryGetValue(chatId, out var player))
                return false;
            player.ApplyClass(playerClass);
            player.RecalculateStats();
            return true;
        }

        public async Task<Player> CompleteCharacterCreation(long chatId)
        {
            if (!_characterCreationProgress.TryGetValue(chatId, out var player))
                return null;

            player.CurrentLocation = "start";
            player.PositionX = 5;
            player.PositionY = 5;

            await _databaseService.SavePlayerAsync(player);
            _playerManager.AddOrUpdatePlayer(player);
            _characterCreationProgress.Remove(chatId);
            return player;
        }
    }
}