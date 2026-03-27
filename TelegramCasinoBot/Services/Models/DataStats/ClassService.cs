using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Stats;
using TelegramCasinoBot.Models.Stats.JsonR;

namespace TelegramCasinoBot.Services.Data
{
    public class ClassService : IClassService
    {
        private readonly ILogger<ClassService> _logger;
        private readonly IClassRepository _repository;

        public ClassService(ILogger<ClassService> logger, IClassRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<IReadOnlyList<Class>> GetAllClassesAsync()
        {
            _logger.LogDebug("Начало GetAllClassesAsync");
            try
            {
                return await _repository.GetAllClassesAsync();
            }
            finally
            {
                _logger.LogDebug("GetAllClassesAsync завершён");
            }
        }

        public async Task<Class> GetClassByIdAsync(int id)
        {
            _logger.LogDebug("Начало GetClassByIdAsync для id {Id}", id);
            try
            {
                return await _repository.GetClassByIdAsync(id);
            }
            finally
            {
                _logger.LogDebug("GetClassByIdAsync завершён для id {Id}", id);
            }
        }

        public async Task<bool> ClassExistsAsync(int id)
        {
            var cls = await _repository.GetClassByIdAsync(id);
            return cls != null;
        }
    }
}