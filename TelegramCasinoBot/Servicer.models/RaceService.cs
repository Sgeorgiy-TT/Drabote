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

            races["human"] = new Race("human", "Человек")
            {
                Description = "Универсальная раса с балансом всех характеристик",
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new List<string> { "Адаптивность" }
            };

            races["elf"] = new Race("elf", "Эльф")
            {
                Description = "Древняя раса с affinity к магии",
                HealthBonus = -10,
                ManaBonus = 50,
                MagicDamageMultiplier = 1.05,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new List<string> { "Магическая affinity" }
            };

            races["orc"] = new Race("orc", "Орк")
            {
                Description = "Сильная и выносливая раса",
                HealthBonus = 20,
                ManaBonus = -20,
                StaminaBonus = 10,
                MeleeDamageMultiplier = 1.1,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new List<string> { "Берсерк" }
            };

            races["dwarf"] = new Race("dwarf", "Гном")
            {
                Description = "Крепкие и устойчивые бойцы",
                HealthBonus = 10,
                DefenseBonus = 20,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new List<string> { "Устойчивость" }
            };

            races["dragonkin"] = new Race("dragonkin", "Драконид")
            {
                Description = "Потомки древних драконов",
                ManaBonus = 20,
                DefenseBonus = 10,
                AvailableGenders = new[] { "Male", "Female" },
                SpecialAbilities = new List<string> { "Огненный шар" }
            };

            return races;
        }
    }
}