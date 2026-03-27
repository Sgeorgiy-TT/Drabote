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
    public interface IClassRepository
    {
        Task<IReadOnlyList<Class>> GetAllClassesAsync();
        Task<Class> GetClassByIdAsync(int id);
    }

    public class JsonClassRepository : IClassRepository
    {
        private readonly ILogger<JsonClassRepository> _logger;
        private readonly string _filePath;
        private List<Class> _classes;

        public JsonClassRepository(ILogger<JsonClassRepository> logger)
        {
            _logger = logger;
            _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Classes.json");
            LoadClasses();
        }

        private void LoadClasses()
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var classesList = JsonSerializer.Deserialize<ClassesList>(json);
                _classes = classesList?.Classes ?? new List<Class>();
                _logger.LogInformation("Загружено {Count} классов", _classes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки классов из JSON");
                _classes = new List<Class>();
            }
        }

        public Task<IReadOnlyList<Class>> GetAllClassesAsync()
        {
            _logger.LogDebug("Начало GetAllClassesAsync");
            try
            {
                return Task.FromResult<IReadOnlyList<Class>>(_classes);
            }
            finally
            {
                _logger.LogDebug("GetAllClassesAsync завершён, возвращено {Count} классов", _classes.Count);
            }
        }

        public Task<Class> GetClassByIdAsync(int id)
        {
            _logger.LogDebug("Начало GetClassByIdAsync для id {Id}", id);
            try
            {
                var cls = _classes.Find(c => c.Id == id);
                if (cls == null)
                    _logger.LogWarning("Класс с id {Id} не найден", id);
                return Task.FromResult(cls);
            }
            finally
            {
                _logger.LogDebug("GetClassByIdAsync завершён для id {Id}", id);
            }
        }
    }
}