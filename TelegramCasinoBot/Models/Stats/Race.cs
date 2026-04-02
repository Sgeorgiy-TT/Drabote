using System.Collections.Generic;

namespace TelegramCasinoBot.Models.Character
{
    public class Race : CharacterStats
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; set; }
        public string[] AvailableGenders { get; set; }
        public List<string> SpecialAbilities { get; } = new();

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
}