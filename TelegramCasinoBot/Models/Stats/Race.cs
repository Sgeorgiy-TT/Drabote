using System.Collections.Generic;
using TelegramCasinoBot.Models.Character;

public class Race : CharacterStats
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string[] AvailableGenders { get; set; }
    public List<string> SpecialAbilities { get; set; } = new();

    public Race() { }
    

    public Race(int id, string name,
        int healthBonus, int manaBonus, int staminaBonus, int defenseBonus,
        double expMultiplier, double meleeMultiplier, double rangedMultiplier, double magicMultiplier)
        : base(healthBonus, manaBonus, staminaBonus, defenseBonus,
               expMultiplier, meleeMultiplier, rangedMultiplier, magicMultiplier)
    {
        Id = id;
        Name = name;
    }
}