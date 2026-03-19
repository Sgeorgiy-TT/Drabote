using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TelegramCasinoBot.Models.Stats;
using TelegramCasinoBot.Services.Models.Data;

namespace TelegramCasinoBot.Services.Models.DataStats
{
    public class ClassService : IClassService
    {
        private readonly ILogger<ClassService> _logger;
        private readonly Dictionary<string, Class> _classes;

        public ClassService(ILogger<ClassService> logger)
        {
            _logger = logger;
            _classes = InitializeClasses();
            _logger.LogInformation("Загружено {Count} классов", _classes.Count);
        }

        public IReadOnlyList<Class> GetAllClasses() => _classes.Values.ToList();

        public Class GetClassById(string id) => _classes.TryGetValue(id, out var cls) ? cls : null;

        public bool ClassExists(string id) => _classes.ContainsKey(id);

        private Dictionary<string, Class> InitializeClasses()
        {
            var classes = new Dictionary<string, Class>();

            classes["warrior"] = new Class("warrior", "Воин")
            {
                Description = "Мастер ближнего боя",
                HealthBonus = 20,
                StaminaBonus = 20,
                DefenseBonus = 10,
                MeleeDamageMultiplier = 1.1,
                StartingAbilities = new List<string> { "Обычный удар", "Усиленный удар" },
                PreferredWeaponTypes = new[] { "Меч", "Топор", "Булава" }
            };

            classes["archer"] = new Class("archer", "Лучник")
            {
                Description = "Стрелок на дальних дистанциях",
                ManaBonus = 10,
                StaminaBonus = 10,
                DefenseBonus = 5,
                RangedDamageMultiplier = 1.1,
                StartingAbilities = new List<string> { "Обычный выстрел", "Усиленный выстрел" },
                PreferredWeaponTypes = new[] { "Лук", "Арбалет" }
            };

            classes["mage"] = new Class("mage", "Маг")
            {
                Description = "Повелитель магических искусств",
                HealthBonus = -20,
                ManaBonus = 30,
                MagicDamageMultiplier = 1.1,
                StartingAbilities = new List<string> { "Магический выстрел", "Усиленный магический выстрел" },
                PreferredWeaponTypes = new[] { "Посох", "Жезл", "Книга заклинаний" }
            };

            return classes;
        }
    }
}