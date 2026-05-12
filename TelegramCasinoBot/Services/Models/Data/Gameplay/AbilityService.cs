using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelegramCasinoBot.Models.Gameplay;

namespace TelegramCasinoBot.Services.Models.Data.Gameplay
{
    public class AbilityService
    {
        private readonly ILogger<AbilityService> _logger;
        private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "Abilities.json");
        private List<Ability> _playerAbilities;
        private List<Ability> _mobAbilities;
        private List<Ability> _bossAbilities;
        private List<Ability> _gameAbilities;

        public AbilityService(ILogger<AbilityService> logger)
        {
            _logger = logger;
            LoadAbilities();
        }

        private void LoadAbilities()
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<AbilitiesContainer>(json);
                _playerAbilities = data?.PlayerAbilities ?? new List<Ability>();
                _mobAbilities = data?.MobAbilities ?? new List<Ability>();
                _bossAbilities = data?.BossAbilities ?? new List<Ability>();
                _logger.LogInformation("Загружено способностей: игрок={0}, мобы={1}, боссы={2}",
                    _playerAbilities.Count, _mobAbilities.Count, _bossAbilities.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки способностей");
                _playerAbilities = new List<Ability>();
                _mobAbilities = new List<Ability>();
                _bossAbilities = new List<Ability>();
            }
        }

        public List<Ability> GetPlayerAbilities() => _playerAbilities;
        public List<Ability> GetMobAbilities() => _mobAbilities;
        public List<Ability> GetBossAbilities() => _bossAbilities;

        public List<Ability> GetAbilitiesForMob(int level, string race = null, string className = null)
        {
            return _mobAbilities.Where(a => a.MinLevel <= level &&
                (string.IsNullOrEmpty(a.RequiredRace) || a.RequiredRace == race) &&
                (string.IsNullOrEmpty(a.RequiredClass) || a.RequiredClass == className)).ToList();
        }

        public List<Ability> GetAbilitiesForBoss(int level)
        {
            return _bossAbilities.Where(a => a.MinLevel <= level).ToList();
        }
        public List<Ability> GetAbilitiesByNames(List<string> names)
        {
            return _playerAbilities.Where(a => names.Contains(a.Name)).ToList();
        }
    }

    internal class AbilitiesContainer
    {
        public List<Ability> PlayerAbilities { get; set; }
        public List<Ability> MobAbilities { get; set; }
        public List<Ability> BossAbilities { get; set; }
    }
}