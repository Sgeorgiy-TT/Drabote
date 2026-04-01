using System;
using System.Collections.Generic;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Models.Stats;

namespace TelegramCasinoBot.Models.Character
{
    public class BaseStats
    {
        public int Health { get; set; } = 100;
        public int Mana { get; set; } = 50;
        public int Stamina { get; set; } = 100;
        public int Defense { get; set; } = 10;
        public int Experience { get; set; } = 0;
        public int Level { get; set; } = 1;

        public BaseStats Clone() => new BaseStats
        {
            Health = this.Health,
            Mana = this.Mana,
            Stamina = this.Stamina,
            Defense = this.Defense,
            Experience = this.Experience,
            Level = this.Level
        };
    }

    public class CombatState
    {
        public int BossHealth { get; set; }
        public int LastBossMessageId { get; set; }
        public int LastMessageId { get; set; }
    }

    public class Player
    {
        public long ChatId { get; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Race { get; set; }
        public string Class { get; set; }
        public string IconPath { get; set; }

        public string CurrentLocation { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }

        public int Health { get; set; }
        public int Mana { get; set; }
        public int Stamina { get; set; }

        public BaseStats BaseStats { get; private set; }

        public CombatState CombatState { get; set; } = new CombatState();

        public List<string> Inventory { get; init; } = new();
        public List<string> Abilities { get; init; } = new();
        public List<string> QuestCompleted { get; init; } = new();
        public Dictionary<string, List<Position>> ExploredAreas { get; init; } = new();

        private List<CharacterStats> _modifiers = new();

        private static readonly BaseStats DefaultBaseStats = new();

        public Player(long chatId)
        {
            ChatId = chatId;
            BaseStats = DefaultBaseStats.Clone();
            Health = BaseStats.Health;
            Mana = BaseStats.Mana;
            Stamina = BaseStats.Stamina;
            CurrentLocation = "start";
            PositionX = 5;
            PositionY = 5;
        }

        public Player(long chatId, string name, string gender, Race race, Class playerClass, string iconName = null)
        {
            ChatId = chatId;
            Name = name;
            Gender = gender;
            Race = race.Name;
            Class = playerClass.Name;
            IconPath = iconName;

            CurrentLocation = "start";
            PositionX = 5;
            PositionY = 5;

            BaseStats = DefaultBaseStats.Clone();
            _modifiers.Add(race);
            _modifiers.Add(playerClass);
            RecalculateStats();

            Health = BaseStats.Health;
            Mana = BaseStats.Mana;
            Stamina = BaseStats.Stamina;
        }

        public void AddModifier(CharacterStats modifier)
        {
            _modifiers.Add(modifier);
            RecalculateStats();
            Health = Math.Min(Health, BaseStats.Health);
            Mana = Math.Min(Mana, BaseStats.Mana);
            Stamina = Math.Min(Stamina, BaseStats.Stamina);
        }

        public void RemoveModifier(CharacterStats modifier)
        {
            if (_modifiers.Remove(modifier))
            {
                RecalculateStats();
                Health = Math.Min(Health, BaseStats.Health);
                Mana = Math.Min(Mana, BaseStats.Mana);
                Stamina = Math.Min(Stamina, BaseStats.Stamina);
            }
        }

        private void RecalculateStats()
        {
            BaseStats = DefaultBaseStats.Clone();

            foreach (var stat in _modifiers)
            {
                BaseStats.Health += stat.HealthBonus;
                BaseStats.Mana += stat.ManaBonus;
                BaseStats.Stamina += stat.StaminaBonus;
                BaseStats.Defense += stat.DefenseBonus;
            }

            BaseStats.Health = Math.Max(1, BaseStats.Health);
            BaseStats.Mana = Math.Max(0, BaseStats.Mana);
            BaseStats.Stamina = Math.Max(0, BaseStats.Stamina);
            BaseStats.Defense = Math.Max(0, BaseStats.Defense);
        }

        public int GetTotalHealthBonus()
        {
            int total = 0;
            foreach (var stat in _modifiers)
                total += stat.HealthBonus;
            return total;
        }

        public int GetTotalManaBonus()
        {
            int total = 0;
            foreach (var stat in _modifiers)
                total += stat.ManaBonus;
            return total;
        }

        public int GetTotalStaminaBonus()
        {
            int total = 0;
            foreach (var stat in _modifiers)
                total += stat.StaminaBonus;
            return total;
        }

        public int GetTotalDefenseBonus()
        {
            int total = 0;
            foreach (var stat in _modifiers)
                total += stat.DefenseBonus;
            return total;
        }

        public double GetTotalExperienceMultiplier()
        {
            double total = 1.0;
            foreach (var stat in _modifiers)
                total *= stat.ExperienceMultiplier;
            return total;
        }

        public double GetTotalMeleeDamageMultiplier()
        {
            double total = 1.0;
            foreach (var stat in _modifiers)
                total *= stat.MeleeDamageMultiplier;
            return total;
        }

        public double GetTotalRangedDamageMultiplier()
        {
            double total = 1.0;
            foreach (var stat in _modifiers)
                total *= stat.RangedDamageMultiplier;
            return total;
        }

        public double GetTotalMagicDamageMultiplier()
        {
            double total = 1.0;
            foreach (var stat in _modifiers)
                total *= stat.MagicDamageMultiplier;
            return total;
        }

        public double GetExplorationProgress(string locationId, GameWorld world)
        {
            if (!ExploredAreas.ContainsKey(locationId)) return 0;
            if (!world.Locations.ContainsKey(locationId)) return 0;
            var location = world.Locations[locationId];
            var totalCells = location.Width * location.Height;
            var exploredCells = ExploredAreas[locationId].Count;
            return (double)exploredCells / totalCells * 100;
        }
    }
}