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
    //вынести отдельно
    public interface IRaceRepository
    {
        Task<IReadOnlyList<Race>> GetAllRacesAsync();
        Task<Race> GetRaceByIdAsync(int id);
    }

    public class JsonRaceRepository : IRaceRepository
    {
        private readonly ILogger<JsonRaceRepository> _logger;
        private readonly string _filePath =Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "Races.json"); 
        private List<Race> _races;

        public Task<IReadOnlyList<Race>> GetAllRacesAsync()
        {
            if (_races == null)
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var racesList = JsonSerializer.Deserialize<RacesList>(json);
                    _races = racesList?.Races ?? new List<Race>();
                    _logger.LogInformation("Загружено {Count} рас", _races.Count);
                }
                //обработать отдельно
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
            var race = _races.Find(r => r.Id == id);
            return Task.FromResult(race);
        }
    }
}