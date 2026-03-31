using System;
using System.Collections.Generic;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Models.Stats;

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
    public int MaxHealth { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public int Stamina { get; set; }
    public int MaxStamina { get; set; }
    public int Defense { get; set; }
    public int Experience { get; set; }
    public int Level { get; set; }
    public string HairType { get; set; }
    public string HairColor { get; set; }
    public string EyeColor { get; set; }
    public string SkinColor { get; set; }
    public string Clothing { get; set; }
    public int BossHealth { get; set; }
    public int LastBossMessageId { get; set; }
    public int LastMessageId { get; set; }
    public double ExperienceMultiplier { get; set; }
    public double MeleeDamageMultiplier { get; set; }
    public double RangedDamageMultiplier { get; set; }
    public double MagicDamageMultiplier { get; set; }

    public List<string> Inventory { get; init; } = new List<string>();
    public List<string> Abilities { get; init; } = new List<string>();
    public List<string> QuestCompleted { get; init; } = new List<string>();
    public Dictionary<string, List<Position>> ExploredAreas { get; init; } = new Dictionary<string, List<Position>>();

    public List<CharacterStats> CharacterStatsList { get; } = new List<CharacterStats>();
    private class BaseStats
    {
        public int Health { get; set; } = 100;
        public int Mana { get; set; } = 50;
        public int Stamina { get; set; } = 100;
        public int Defense { get; set; } = 10;
        public int Experience { get; set; } = 0;
        public int Level { get; set; } = 1;
    }
    private static readonly BaseStats DefaultBaseStats = new BaseStats();
    public Player(long chatId)
    {
        ChatId = chatId;
        Health = DefaultBaseStats.Health;
        MaxHealth = DefaultBaseStats.Health;
        Mana = DefaultBaseStats.Mana;
        MaxMana = DefaultBaseStats.Mana;
        Stamina = DefaultBaseStats.Stamina;
        MaxStamina = DefaultBaseStats.Stamina;
        Defense = DefaultBaseStats.Defense;
        Experience = DefaultBaseStats.Experience;
        Level = DefaultBaseStats.Level;
        CurrentLocation = "start";
        PositionX = 5;
        PositionY = 5;
    }

    public Player(long chatId, string name, string gender, Race race, Class characterClass, string iconName = null)
    {
        ChatId = chatId;
        Name = name;
        Gender = gender;
        Race = race.Name;
        Class = characterClass.Name;
        IconPath = iconName;

        Health = DefaultBaseStats.Health;
        MaxHealth = DefaultBaseStats.Health;
        Mana = DefaultBaseStats.Mana;
        MaxMana = DefaultBaseStats.Mana;
        Stamina = DefaultBaseStats.Stamina;
        MaxStamina = DefaultBaseStats.Stamina;
        Defense = DefaultBaseStats.Defense;
        Experience = DefaultBaseStats.Experience;
        Level = DefaultBaseStats.Level;
        CurrentLocation = "start";
        PositionX = 5;
        PositionY = 5;

        CharacterStatsList.Add(race);
        CharacterStatsList.Add(characterClass);

        RecalculateStats();
    }

    public void ApplyRace(Race race)
    {
        Race = race.Name;
        CharacterStatsList.Add(race);
    }

    public void ApplyClass(Class playerClass)
    {
        Class = playerClass.Name;
        CharacterStatsList.Add(playerClass);
    }

    public int GetTotalHealthBonus()
    {
        int total = 0;
        foreach (var stat in CharacterStatsList)
            total += stat.HealthBonus;
        return total;
    }

    public int GetTotalManaBonus()
    {
        int total = 0;
        foreach (var stat in CharacterStatsList)
            total += stat.ManaBonus;
        return total;
    }

    public int GetTotalStaminaBonus()
    {
        int total = 0;
        foreach (var stat in CharacterStatsList)
            total += stat.StaminaBonus;
        return total;
    }

    public int GetTotalDefenseBonus()
    {
        int total = 0;
        foreach (var stat in CharacterStatsList)
            total += stat.DefenseBonus;
        return total;
    }

    public double GetTotalExperienceMultiplier()
    {
        double total = 1.0;
        foreach (var stat in CharacterStatsList)
            total *= stat.ExperienceMultiplier;
        return total;
    }

    public double GetTotalMeleeDamageMultiplier()
    {
        double total = 1.0;
        foreach (var stat in CharacterStatsList)
            total *= stat.MeleeDamageMultiplier;
        return total;
    }

    public double GetTotalRangedDamageMultiplier()
    {
        double total = 1.0;
        foreach (var stat in CharacterStatsList)
            total *= stat.RangedDamageMultiplier;
        return total;
    }

    public double GetTotalMagicDamageMultiplier()
    {
        double total = 1.0;
        foreach (var stat in CharacterStatsList)
            total *= stat.MagicDamageMultiplier;
        return total;
    }
    public void RecalculateStats()
    {
        MaxHealth = DefaultBaseStats.Health + GetTotalHealthBonus();
        Health = Math.Min(Health, MaxHealth);
        MaxMana = DefaultBaseStats.Mana + GetTotalManaBonus();
        Mana = Math.Min(Mana, MaxMana);
        MaxStamina = DefaultBaseStats.Stamina + GetTotalStaminaBonus();
        Stamina = Math.Min(Stamina, MaxStamina);
        Defense = DefaultBaseStats.Defense + GetTotalDefenseBonus();

        ExperienceMultiplier = GetTotalExperienceMultiplier();
        MeleeDamageMultiplier = GetTotalMeleeDamageMultiplier();
        RangedDamageMultiplier = GetTotalRangedDamageMultiplier();
        MagicDamageMultiplier = GetTotalMagicDamageMultiplier();
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