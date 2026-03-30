using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Stats;
using TelegramCasinoBot.Models.Stats.List;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.UI;

namespace TelegramCasinoBot.Models.Stats.JsonR
{
    public class JsonClassRepository : IClassService
    {
        private readonly ILogger<JsonClassRepository> _logger;
        private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "Classes.json");
        private List<Class> _classes;
        public JsonClassRepository(ILogger<JsonClassRepository> logger)
        {
            _logger = logger;
        }
        public Task<IReadOnlyList<Class>> GetAllClassesAsync()
        {
            if (_classes == null)
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
            return Task.FromResult<IReadOnlyList<Class>>(_classes);
        }

        public Task<Class> GetClassByIdAsync(int id)
        {
            var classes = GetAllClassesAsync().Result;
            var cls = classes.FirstOrDefault(c => c.Id == id);
            return Task.FromResult(cls);
        }

        public Task<bool> ClassExistsAsync(int id)
        {
            var cls = GetClassByIdAsync(id).Result;
            return Task.FromResult(cls != null);
        }
    }
}