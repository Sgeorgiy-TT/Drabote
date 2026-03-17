using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using TelegramMetroidvaniaBot.Models;

namespace TelegramMetroidvaniaBot.Services.Data
{
    public class RaceService : IRaceService
    {
        private readonly ILogger<RaceService> _logger;
        private readonly Dictionary<string, Race> _races;

        public RaceService(ILogger<RaceService> logger)
        {
            _logger = logger;
            _races = InitializeRaces();
            _logger.LogInformation("Загружено {Count} рас", _races.Count);
        }

        public IReadOnlyList<Race> GetAllRaces() => _races.Values.ToList();

        public Race GetRaceById(string id) => _races.TryGetValue(id, out var race) ? race : null;

        public bool RaceExists(string id) => _races.ContainsKey(id);

        private Dictionary<string, Race> InitializeRaces()
        {
            var races = new Dictionary<string, Race>();

            races["human"] = new Race
            {
                Id = "human",
                Name = "Человек",
                Description = "Универсальная раса с балансом всех характеристик",
                HealthBonus = 0,
                ManaBonus = 0,
                StaminaBonus = 0,
                DefenseBonus = 0,
                ExperienceMultiplier = 1.1,
                MeleeDamageBonus = 0,
                RangedDamageBonus = 0,
                MagicDamageBonus = 0,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new[] { "Адаптивность" }
            };

            races["elf"] = new Race
            {
                Id = "elf",
                Name = "Эльф",
                Description = "Древняя раса с affinity к магии",
                HealthBonus = -10,
                ManaBonus = 50,
                StaminaBonus = 0,
                DefenseBonus = 0,
                ExperienceMultiplier = 1.0,
                MeleeDamageBonus = 0,
                RangedDamageBonus = 0,
                MagicDamageBonus = 1.05,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new[] { "Магическая affinity" }
            };

            races["orc"] = new Race
            {
                Id = "orc",
                Name = "Орк",
                Description = "Сильная и выносливая раса",
                HealthBonus = 20,
                ManaBonus = -20,
                StaminaBonus = 10,
                DefenseBonus = 0,
                ExperienceMultiplier = 1.0,
                MeleeDamageBonus = 1.1,
                RangedDamageBonus = 0,
                MagicDamageBonus = 0,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new[] { "Берсерк" }
            };

            races["dwarf"] = new Race
            {
                Id = "dwarf",
                Name = "Гном",
                Description = "Крепкие и устойчивые бойцы",
                HealthBonus = 10,
                ManaBonus = 0,
                StaminaBonus = 0,
                DefenseBonus = 20,
                ExperienceMultiplier = 1.0,
                MeleeDamageBonus = 0,
                RangedDamageBonus = 0,
                MagicDamageBonus = 0,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new[] { "Устойчивость" }
            };

            races["dragonkin"] = new Race
            {
                Id = "dragonkin",
                Name = "Драконид",
                Description = "Потомки древних драконов",
                HealthBonus = 0,
                ManaBonus = 20,
                StaminaBonus = 0,
                DefenseBonus = 10,
                ExperienceMultiplier = 1.0,
                MeleeDamageBonus = 0,
                RangedDamageBonus = 0,
                MagicDamageBonus = 0,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new[] { "Огненный шар" }
            };

            return races;
        }
    }
}