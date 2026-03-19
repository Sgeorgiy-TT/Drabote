using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TelegramMetroidvaniaBot.Models;

namespace TelegramMetroidvaniaBot.Services.Data
{
    public class ClassService : IClassService
    {
        private readonly ILogger<ClassService> _logger;
        private readonly Dictionary<string, CharacterClass> _classes;

        public ClassService(ILogger<ClassService> logger)
        {
            _logger = logger;
            _classes = InitializeClasses();
            _logger.LogInformation("Загружено {Count} классов", _classes.Count);
        }

        public IReadOnlyList<CharacterClass> GetAllClasses() => _classes.Values.ToList();

        public CharacterClass GetClassById(string id) => _classes.TryGetValue(id, out var cls) ? cls : null;

        public bool ClassExists(string id) => _classes.ContainsKey(id);

        private Dictionary<string, CharacterClass> InitializeClasses()
        {
            var classes = new Dictionary<string, CharacterClass>();

            classes["warrior"] = new CharacterClass("warrior", "Воин")
            {
                Description = "Мастер ближнего боя",
                HealthBonus = 20,
                StaminaBonus = 20,
                DefenseBonus = 10,
                MeleeDamageMultiplier = 1.1,
                StartingAbilities = new List<string> { "Обычный удар", "Усиленный удар" },
                PreferredWeaponTypes = new[] { "Меч", "Топор", "Булава" }
            };

            classes["archer"] = new CharacterClass("archer", "Лучник")
            {
                Description = "Стрелок на дальних дистанциях",
                ManaBonus = 10,
                StaminaBonus = 10,
                DefenseBonus = 5,
                RangedDamageMultiplier = 1.1,
                StartingAbilities = new List<string> { "Обычный выстрел", "Усиленный выстрел" },
                PreferredWeaponTypes = new[] { "Лук", "Арбалет" }
            };

            classes["mage"] = new CharacterClass("mage", "Маг")
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