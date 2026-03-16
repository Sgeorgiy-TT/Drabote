using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Logging;
using TelegramMetroidvaniaBot.Models;

namespace TelegramMetroidvaniaBot.Services
{
    public class DatabaseService
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _dataFilePath;
        private List<PlayerSave> _playerSaves;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger;

            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }

            _dataFilePath = Path.Combine(_dataDirectory, "player_saves.json");
            LoadSaves();
        }

        private void LoadSaves()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var json = File.ReadAllText(_dataFilePath);
                    _playerSaves = JsonSerializer.Deserialize<List<PlayerSave>>(json) ?? new List<PlayerSave>();
                    _logger.LogInformation("Загружено {Count} сохранений", _playerSaves.Count);
                }
                else
                {
                    _playerSaves = new List<PlayerSave>();
                    _logger.LogInformation("Файл сохранений не найден, создан новый список");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки сохранений: {Message}", ex.Message);
                _playerSaves = new List<PlayerSave>();
            }
        }

        private async Task SaveSavesAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_playerSaves, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_dataFilePath, json);
                _logger.LogDebug("Сохранено {Count} записей", _playerSaves.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения: {Message}", ex.Message);
            }
        }

        public async Task<PlayerSave> GetPlayerSaveAsync(long chatId)
        {
            return _playerSaves.FirstOrDefault(p => p.ChatId == chatId && p.IsActive);
        }

        public async Task<bool> SavePlayerAsync(Player player)
        {
            try
            {
                var existingSave = await GetPlayerSaveAsync(player.ChatId);

                if (existingSave != null)
                {
                    existingSave.CurrentLocation = player.CurrentLocation;
                    existingSave.Health = player.Health;
                    existingSave.MaxHealth = player.MaxHealth;
                    existingSave.Mana = player.Mana;
                    existingSave.MaxMana = player.MaxMana;
                    existingSave.Experience = player.Experience;
                    existingSave.Level = player.Level;
                    existingSave.LastPlayed = DateTime.Now;
                    existingSave.PlayTimeMinutes += 1;
                    _logger.LogDebug("Обновлено сохранение для chatId: {ChatId}", player.ChatId);
                }
                else
                {
                    var newSave = new PlayerSave
                    {
                        ChatId = player.ChatId,
                        PlayerName = $"Игрок_{player.ChatId}",
                        CurrentLocation = player.CurrentLocation,
                        Health = player.Health,
                        MaxHealth = player.MaxHealth,
                        Mana = player.Mana,
                        MaxMana = player.MaxMana,
                        Experience = player.Experience,
                        Level = player.Level,
                        CreatedAt = DateTime.Now,
                        LastPlayed = DateTime.Now,
                        IsActive = true,
                        PlayTimeMinutes = 0
                    };
                    _playerSaves.Add(newSave);
                    _logger.LogDebug("Создано новое сохранение для chatId: {ChatId}", player.ChatId);
                }

                await SaveSavesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения игрока: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<List<PlayerSave>> GetPlayerSavesAsync(long chatId)
        {
            return _playerSaves
                .Where(p => p.ChatId == chatId)
                .OrderByDescending(p => p.LastPlayed)
                .ToList();
        }

        public async Task<bool> DeleteSaveAsync(long chatId)
        {
            var save = await GetPlayerSaveAsync(chatId);
            if (save != null)
            {
                save.IsActive = false;
                await SaveSavesAsync();
                return true;
            }
            return false;
        }
    }
}