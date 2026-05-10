using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelegramCasinoBot.Services.Models.Data.Creation;

namespace TelegramCasinoBot.Services.JsonR
{
    public class JsonRaceRepository : IRaceService
    {
        private readonly ILogger<JsonRaceRepository> _logger;
        private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "Races.json");
        private List<Race> _races;

        public JsonRaceRepository(ILogger<JsonRaceRepository> logger)
        {
            _logger = logger;
        }

        public Task<IReadOnlyList<Race>> GetAllRacesAsync()
        {
            if (_races == null)
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    _races = JsonSerializer.Deserialize<List<Race>>(json) ?? new List<Race>();
                    _logger.LogInformation("Загружено {Count} рас", _races.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка загрузки рас из JSON");
                    _races = new List<Race>();
                }
            }
            return Task.FromResult<IReadOnlyList<Race>>(_races);
        }

        public Task<Race> GetRaceByIdAsync(int id)
        {
            var races = GetAllRacesAsync().Result;
            var race = races.FirstOrDefault(r => r.Id == id);
            return Task.FromResult(race);
        }

        public Task<bool> RaceExistsAsync(int id)
        {
            var race = GetRaceByIdAsync(id).Result;
            return Task.FromResult(race != null);
        }

        public async Task<Race> GetRaceByNameAsync(string name)
        {
            var races = await GetAllRacesAsync();
            return races.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}