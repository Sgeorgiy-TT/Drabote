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
        private class CreationData
        {
            public string Name { get; set; }
            public string Gender { get; set; }
            public Race Race { get; set; }
            public Class Class { get; set; }
            public string IconPath { get; set; }
        }

        private readonly DatabaseService _databaseService;
        private readonly PlayerManager _playerManager;
        private readonly ILogger<CharacterCreationService> _logger;
        private readonly Dictionary<long, CreationData> _creationData = new();

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
            _creationData[chatId] = new CreationData();
        }

        public bool IsInCharacterCreation(long chatId) => _creationData.ContainsKey(chatId);

        public void SetName(long chatId, string name)
        {
            if (_creationData.TryGetValue(chatId, out var data))
                data.Name = name;
        }

        public void SetGender(long chatId, string gender)
        {
            if (_creationData.TryGetValue(chatId, out var data))
                data.Gender = gender;
        }

        public void SetIconPath(long chatId, string iconPath)
        {
            if (_creationData.TryGetValue(chatId, out var data))
                data.IconPath = iconPath;
        }

        public void ApplyRace(long chatId, Race race)
        {
            if (_creationData.TryGetValue(chatId, out var data))
                data.Race = race;
        }

        public void ApplyClass(long chatId, Class playerClass)
        {
            if (_creationData.TryGetValue(chatId, out var data))
                data.Class = playerClass;
        }

        public async Task<Player> CompleteCharacterCreation(long chatId)
        {
            if (!_creationData.TryGetValue(chatId, out var data))
                return null;

            var player = new Player(chatId, data.Name, data.Gender, data.Race, data.Class, data.IconPath);

            await _databaseService.SavePlayerAsync(player);
            _playerManager.AddOrUpdatePlayer(player);
            _creationData.Remove(chatId);
            return player;
        }
        public Player GetCharacterInProgress(long chatId)
        {
            if (_creationData.TryGetValue(chatId, out var data))
            {
                var player = new Player(chatId);
                player.Name = data.Name;
                player.Gender = data.Gender;
                player.Race = data.Race?.Name;
                player.Class = data.Class?.Name;
                player.IconPath = data.IconPath;
                return player;
            }
            return null;
        }

        public (string Name, string Gender) GetPlayerCreationData(long chatId)
        {
            if (_creationData.TryGetValue(chatId, out var data))
                return (data.Name, data.Gender);
            return (null, null);
        }
    }
}