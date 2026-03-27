using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Stats;
using TelegramCasinoBot.Models.Stats.List;

namespace TelegramCasinoBot.Models.Stats.JsonR
{
    public interface IRaceRepository
    {
        Task<IReadOnlyList<Race>> GetAllRacesAsync();
        Task<Race> GetRaceByIdAsync(int id);
    }

    public class JsonRaceRepository : IRaceRepository
    {
        private readonly ILogger<JsonRaceRepository> _logger;
        private readonly string _filePath;
        private List<Race> _races;

        public JsonRaceRepository(ILogger<JsonRaceRepository> logger)
        {
            _logger = logger;
            _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Races.json");
            LoadRaces();
        }

        private void LoadRaces()
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var racesList = JsonSerializer.Deserialize<RacesList>(json);
                _races = racesList?.Races ?? new List<Race>();
                _logger.LogInformation("Загружено {Count} рас", _races.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки рас из JSON");
                _races = new List<Race>();
            }
        }

        public Task<IReadOnlyList<Race>> GetAllRacesAsync()
        {
            return Task.FromResult<IReadOnlyList<Race>>(_races);
        }

        public Task<Race> GetRaceByIdAsync(int id)
        {
            var race = _races.Find(r => r.Id == id);
            return Task.FromResult(race);
        }
    }
}