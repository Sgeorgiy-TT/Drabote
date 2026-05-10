using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Transactions;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Models.DataStats;


public partial class Player : CharacterStats
{
    public long ChatId { get; set; }
    public string Name { get; set; }
    public string Gender { get; set; }
    public string Race { get; set; }
    public string Class { get; set; }
    public string IconPath { get; set; }
    public string CurrentLocation { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public CharacterAttribute Health { get; private set; } = new CharacterAttribute(0, 0);
    public CharacterAttribute Mana { get; private set; } = new CharacterAttribute(0, 0);
    public CharacterAttribute Stamina { get; private set; } = new CharacterAttribute(0, 0);
    public int Defense { get; set; }
    public int Experience { get; set; }
    public int Level { get; set; }
    public int LastBossMessageId { get; set; }
    public int LastMessageId { get; set; }
    public int BossHealth { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastPlayed { get; set; } = DateTime.Now;
    public Dictionary<string, List<MobInstance>> LocationMobs { get; set; } = new();
    public int PlayTimeMinutes { get; set; } = 0;
    [JsonIgnore]
    public double ExperienceMultiplier => GetTotalExperienceMultiplier();
    [JsonIgnore]
    public double MeleeDamageMultiplier => GetTotalMeleeDamageMultiplier();
    [JsonIgnore]
    public double RangedDamageMultiplier => GetTotalRangedDamageMultiplier();
    [JsonIgnore]
    public double MagicDamageMultiplier => GetTotalMagicDamageMultiplier();

    public List<string> Inventory { get; init; } = new List<string>();
    public List<string> Abilities { get; init; } = new List<string>();
    public List<string> QuestCompleted { get; init; } = new List<string>();
    public Dictionary<string, List<Position>> ExploredAreas { get; init; } = new Dictionary<string, List<Position>>();

    public List<CharacterStats> CharacterStatsList { get; } = new List<CharacterStats>();
    protected Player() 
    {
        Inventory = new List<string>();
        Abilities = new List<string>();
        QuestCompleted = new List<string>();
        ExploredAreas = new Dictionary<string, List<Position>>();
        CharacterStatsList = new List<CharacterStats>();
        Health = new CharacterAttribute(0, 0);
        Mana = new CharacterAttribute(0, 0);
        Stamina = new CharacterAttribute(0, 0);
    }
    
    public Player(long chatId, string name, string gender, Race race, Class characterClass, string iconName)
         : base(100, 50, 100, 10, 1.0, 1.0, 1.0, 1.0)
    {
        ChatId = chatId;
        Name = name;
        Gender = gender;
        Race = race?.Name;
        Class = characterClass?.Name;
        IconPath = iconName;
        CurrentLocation = "start";
        PositionX = 5;
        PositionY = 5;

        Health = new CharacterAttribute(HealthBonus, HealthBonus);
        Mana = new CharacterAttribute(ManaBonus, ManaBonus);
        Stamina = new CharacterAttribute(StaminaBonus, StaminaBonus);

        Experience = 0;
        Level = 1;

        CharacterStatsList.Add(race);
        CharacterStatsList.Add(characterClass);

        RecalculateStats();
        Health.Current = Health.Max;
        Mana.Current = Mana.Max;
        Stamina.Current = Stamina.Max;
    }

    public Player(long chatId, string name, string gender, Race race, Class characterClass, string iconName, int experience, int level, string currentLocation, int positionX, int positionY)
    : this(chatId, name, gender, race, characterClass, iconName)
    {
        this.Experience = experience;
        this.Level = level;
        this.CurrentLocation = currentLocation;
        this.PositionX = positionX;
        this.PositionY = positionY;
        RecalculateStats();
    }

    public int GetTotalHealthBonus()
    {
        int total = 0;
        foreach (CharacterStats stat in CharacterStatsList)
            total += stat.HealthBonus;
        return total;
    }

    public int GetTotalManaBonus()
    {
        int total = 0;
        foreach (CharacterStats stat in CharacterStatsList)
            total += stat.ManaBonus;
        return total;
    }

    public int GetTotalStaminaBonus()
    {
        int total = 0;
        foreach (CharacterStats stat in CharacterStatsList)
            total += stat.StaminaBonus;
        return total;
    }

    public int GetTotalDefenseBonus()
    {
        int total = 0;
        foreach (CharacterStats stat in CharacterStatsList)
            total += stat.DefenseBonus;
        return total;
    }

    public double GetTotalExperienceMultiplier()
    {
        double total = 1.0;
        foreach (CharacterStats stat in CharacterStatsList)
            total *= stat.ExperienceMultiplier;
        return total;
    }

    public double GetTotalMeleeDamageMultiplier()
    {
        double total = 1.0;
        foreach (CharacterStats stat in CharacterStatsList)
            total *= stat.MeleeDamageMultiplier;
        return total;
    }

    public double GetTotalRangedDamageMultiplier()
    {
        double total = 1.0;
        foreach (CharacterStats stat in CharacterStatsList)
            total *= stat.RangedDamageMultiplier;
        return total;
    }

    public double GetTotalMagicDamageMultiplier()
    {
        double total = 1.0;
        foreach (CharacterStats stat in CharacterStatsList)
            total *= stat.MagicDamageMultiplier;
        return total;
    }

    public void RecalculateStats()
    {
        Health.Max = HealthBonus + GetTotalHealthBonus();
        Mana.Max = ManaBonus + GetTotalManaBonus();
        Stamina.Max = StaminaBonus + GetTotalStaminaBonus();
        Defense = DefenseBonus + GetTotalDefenseBonus();
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
    public void AddExperience(int exp)
    {
        Experience += exp;
        var expForNextLevel = PlayerService.CalculateExpForNextLevel(Level);
        while (Experience >= expForNextLevel)
        {
            LevelUp();
            expForNextLevel = PlayerService.CalculateExpForNextLevel(Level);
        }
    }

    private void LevelUp()
    {
        Level++;
        int healthBonus = 20, manaBonus = 10, staminaBonus = 5;
        Health.Max += healthBonus;
        Mana.Max += manaBonus;
        Stamina.Max += staminaBonus;
        Health.Current = Health.Max;
        Mana.Current = Mana.Max;
        Stamina.Current = Stamina.Max;
    }
}
