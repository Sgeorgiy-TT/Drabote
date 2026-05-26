using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Models.Data.Creation;
using TelegramCasinoBot.Services.Models.Data.Gameplay;

namespace TelegramCasinoBot.Services.Infrastructure
{
    public class DatabaseService
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _dataFilePath;
        private List<Player> _players;
        private readonly ILogger<DatabaseService> _logger;
        private readonly AbilityService _abilityService;
        private readonly IRaceService _raceService;
        private readonly IClassService _classService;
        private readonly ItemService _itemService;

        public DatabaseService(ILogger<DatabaseService> logger, AbilityService abilityService, IRaceService raceService, IClassService classService, ItemService itemService)
        {
            _logger = logger;
            _abilityService = abilityService;
            _raceService = raceService;
            _classService = classService;

            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }

            _dataFilePath = Path.Combine(_dataDirectory, "players.json");
            LoadPlayers();
            _itemService = itemService;
        }

        private void LoadPlayers()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var json = File.ReadAllText(_dataFilePath);
                    _players = JsonSerializer.Deserialize<List<Player>>(json) ?? new List<Player>();
                    _logger.LogInformation("Загружено {Count} игроков", _players.Count);
                }
                else
                {
                    _players = new List<Player>();
                    _logger.LogInformation("Файл игроков не найден, создан новый список");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки игроков: {Message}", ex.Message);
                _players = new List<Player>();
            }
        }

        private async Task SavePlayersAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_players, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_dataFilePath, json);
                _logger.LogDebug("Сохранено {Count} игроков", _players.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения: {Message}", ex.Message);
            }
        }

        public async Task<Player> GetPlayerSaveAsync(long chatId)
        {
            var player = _players.FirstOrDefault(p => p.ChatId == chatId && p.IsActive);
            if (player != null)
            {
                int savedHealth = player.Health.Current;
                int savedMana = player.Mana.Current;
                int savedStamina = player.Stamina.Current;

                if (player.LocationMobs == null) player.LocationMobs = new Dictionary<string, List<MobInstance>>();
                if (player.ExploredAreas == null) player.ExploredAreas = new Dictionary<string, List<Position>>();
                if (player.Inventory == null) player.Inventory = new List<string>();
                if (player.AbilityNames == null) player.AbilityNames = new List<string>();
                if (player.QuestCompleted == null) player.QuestCompleted = new List<string>();
                if (player.QuestProgress == null) player.QuestProgress = new List<QuestProgress>();
                if (player.OpenedChests == null) player.OpenedChests = new List<string>();

                player.CharacterStatsList.Clear();
                if (!string.IsNullOrEmpty(player.Race))
                {
                    var race = await _raceService.GetRaceByNameAsync(player.Race);
                    if (race != null) player.CharacterStatsList.Add(race);
                }
                if (!string.IsNullOrEmpty(player.Class))
                {
                    var playerClass = await _classService.GetClassByNameAsync(player.Class);
                    if (playerClass != null) player.CharacterStatsList.Add(playerClass);
                }
                if (player.EquippedWeaponId != 0)
                {
                    var weapon = _itemService.GetItemById(player.EquippedWeaponId);
                    if (weapon != null) player.WeaponBonusDamage = weapon.BaseDamage ?? 0;
                }
                if (player.EquippedArmorId != 0)
                {
                    var armor = _itemService.GetItemById(player.EquippedArmorId);
                    if (armor != null) player.ArmorBonusDefense = armor.Defense ?? 0;
                }
                if (player.AbilityNames.Any() && _abilityService != null)
                {
                    player.LearnedAbilities = _abilityService.GetAbilitiesByNames(player.AbilityNames);
                }

                player.RecalculateStats();

                player.Health.Current = savedHealth;
                player.Mana.Current = savedMana;
                player.Stamina.Current = savedStamina;

                _logger.LogDebug($"Загружен игрок {player.Name}: Health={player.Health.Current}/{player.Health.Max}, Mana={player.Mana.Current}/{player.Mana.Max}, Stamina={player.Stamina.Current}/{player.Stamina.Max}");
            }
            return player;
        }

        public async Task<List<Player>> GetPlayerSavesAsync(long chatId)
        {
            _logger.LogDebug("Начало GetPlayerSavesAsync для chatId {ChatId}", chatId);
            try
            {
                return _players
                    .Where(p => p.ChatId == chatId)
                    .OrderByDescending(p => p.LastPlayed)
                    .ToList();
            }
            finally
            {
                _logger.LogDebug("GetPlayerSavesAsync завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task<bool> SavePlayerAsync(Player player)
        {
            _logger.LogDebug("Начало SavePlayerAsync для chatId {ChatId}", player.ChatId);
            try
            {
                _logger.LogDebug($"Сохраняется игрок {player.Name}, здоровье: {player.Health.Current}/{player.Health.Max}");
                var existing = await GetPlayerSaveAsync(player.ChatId);
                if (existing != null)
                {
                    existing.Name = player.Name;
                    existing.Gender = player.Gender;
                    existing.Race = player.Race;
                    existing.Class = player.Class;
                    existing.CurrentLocation = player.CurrentLocation;
                    existing.PositionX = player.PositionX;
                    existing.PositionY = player.PositionY;
                    existing.Health.Current = player.Health.Current;
                    existing.Health.Max = player.Health.Max;
                    existing.Mana.Current = player.Mana.Current;
                    existing.Mana.Max = player.Mana.Max;
                    existing.Stamina.Current = player.Stamina.Current;
                    existing.Stamina.Max = player.Stamina.Max;
                    existing.Defense = player.Defense;
                    existing.Experience = player.Experience;
                    existing.Level = player.Level;
                    existing.LastPlayed = DateTime.Now;
                    existing.PlayTimeMinutes += 1;
                    existing.IconPath = player.IconPath;
                    existing.Inventory = player.Inventory;
                    existing.AbilityNames = player.LearnedAbilities.Select(a => a.Name).ToList();
                    existing.QuestCompleted = player.QuestCompleted;
                    existing.ExploredAreas = player.ExploredAreas;
                    existing.LocationMobs = player.LocationMobs;
                    existing.SpeedBoost = player.SpeedBoost;
                    existing.Strength = player.Strength;
                    existing.EquippedWeaponId = player.EquippedWeaponId;
                    existing.EquippedArmorId = player.EquippedArmorId;
                    existing.Gold = player.Gold;
                    existing.QuestProgress = player.QuestProgress;
                    existing.CurrentQuestId = player.CurrentQuestId;
                }
                else
                {
                    player.IsActive = true;
                    player.LastPlayed = DateTime.Now;
                    if (player.LocationMobs == null) player.LocationMobs = new Dictionary<string, List<MobInstance>>();
                    if (player.ExploredAreas == null) player.ExploredAreas = new Dictionary<string, List<Position>>();
                    if (player.AbilityNames == null) player.AbilityNames = new List<string>();
                    _players.Add(player);
                }
                await SavePlayersAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения игрока: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteSaveAsync(long chatId)
        {
            _logger.LogDebug("Начало DeleteSaveAsync для chatId {ChatId}", chatId);
            try
            {
                var player = await GetPlayerSaveAsync(chatId);
                if (player != null)
                {
                    player.IsActive = false;
                    await SavePlayersAsync();
                    return true;
                }
                return false;
            }
            finally
            {
                _logger.LogDebug("DeleteSaveAsync завершён для chatId {ChatId}", chatId);

            }
        }
    }
}