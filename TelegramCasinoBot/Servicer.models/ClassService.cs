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

            classes["warrior"] = new CharacterClass
            {
                Id = "warrior",
                Name = "Воин",
                Description = "Мастер ближнего боя",
                HealthBonus = 20,
                ManaBonus = 0,
                StaminaBonus = 20,
                DefenseBonus = 10,
                MeleeDamageMultiplier = 1.1,
                RangedDamageMultiplier = 1.0,
                MagicDamageMultiplier = 1.0,
                StartingAbilities = new[] { "Обычный удар", "Усиленный удар" },
                PreferredWeaponTypes = new[] { "Меч", "Топор", "Булава" }
            };

            classes["archer"] = new CharacterClass
            {
                Id = "archer",
                Name = "Лучник",
                Description = "Стрелок на дальних дистанциях",
                HealthBonus = 0,
                ManaBonus = 10,
                StaminaBonus = 10,
                DefenseBonus = 5,
                MeleeDamageMultiplier = 1.0,
                RangedDamageMultiplier = 1.1,
                MagicDamageMultiplier = 1.0,
                StartingAbilities = new[] { "Обычный выстрел", "Усиленный выстрел" },
                PreferredWeaponTypes = new[] { "Лук", "Арбалет" }
            };

            classes["mage"] = new CharacterClass
            {
                Id = "mage",
                Name = "Маг",
                Description = "Повелитель магических искусств",
                HealthBonus = -20,
                ManaBonus = 30,
                StaminaBonus = 0,
                DefenseBonus = 0,
                MeleeDamageMultiplier = 1.0,
                RangedDamageMultiplier = 1.0,
                MagicDamageMultiplier = 1.1,
                StartingAbilities = new[] { "Магический выстрел", "Усиленный магический выстрел" },
                PreferredWeaponTypes = new[] { "Посох", "Жезл", "Книга заклинаний" }
            };

            return classes;
        }
    }
}