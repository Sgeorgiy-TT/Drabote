using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Stats;
using TelegramCasinoBot.Models.Stats.JsonR;


namespace TelegramCasinoBot.Services.Data
{
    public class RaceService : IRaceService
    {
        private readonly ILogger<RaceService> _logger;//генерацию прямо здесь в блоке инециализации
        private readonly IRaceRepository _repository;

        public RaceService( IRaceRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<Race>> GetAllRacesAsync()
        {
            _logger.LogDebug("Начало GetAllRacesAsync");
            try
            {
                return await _repository.GetAllRacesAsync();
            }
            finally
            {
                _logger.LogDebug("GetAllRacesAsync завершён");
            }
        }

        public async Task<Race> GetRaceByIdAsync(int id)
        {
            _logger.LogDebug("Начало GetRaceByIdAsync для id {Id}", id);
            try
            {
                return await _repository.GetRaceByIdAsync(id);
            }
            finally
            {
                _logger.LogDebug("GetRaceByIdAsync завершён для id {Id}", id);
            }
        }

        public async Task<bool> RaceExistsAsync(int id)
        {
            _logger.LogDebug("Начало RaceExistsAsync для id {Id}", id);
            try
            {
                var race = await _repository.GetRaceByIdAsync(id);
                return race != null;
            }
            finally
            {
                _logger.LogDebug("RaceExistsAsync завершён для id {Id}", id);
            }
        }
    }
}