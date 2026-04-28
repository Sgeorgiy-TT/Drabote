using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelegramCasinoBot.Models.Character;

namespace TelegramCasinoBot.Services.Infrastructure
{
    public class DatabaseService
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _dataFilePath;
        private List<Player> _players;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger;

            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }

            _dataFilePath = Path.Combine(_dataDirectory, "players.json");
            LoadPlayers();
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
            _logger.LogDebug("Начало GetPlayerSaveAsync для chatId {ChatId}", chatId);
            try
            {
                return _players.FirstOrDefault(p => p.ChatId == chatId && p.IsActive);
            }
            finally
            {
                _logger.LogDebug("GetPlayerSaveAsync завершён для chatId {ChatId}", chatId);
            }
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
                var existing = await GetPlayerSaveAsync(player.ChatId);
                if (existing != null)
                {
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
                }
                else
                {
                    player.IsActive = true;
                    player.LastPlayed = DateTime.Now;
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