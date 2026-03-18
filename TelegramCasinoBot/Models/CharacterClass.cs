using System.Collections.Generic;

namespace TelegramMetroidvaniaBot.Models
{
    public class CharacterClass
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int HealthBonus { get; set; }
        public int ManaBonus { get; set; }
        public int StaminaBonus { get; set; }
        public int DefenseBonus { get; set; }
        public double MeleeDamageMultiplier { get; set; }
        public double RangedDamageMultiplier { get; set; }
        public double MagicDamageMultiplier { get; set; }
        public List<string> StartingAbilities { get; set; } = new List<string>();
        public string[] PreferredWeaponTypes { get; set; }

        public CharacterClass(string id, string name)
        {
            Id = id;
            Name = name;
            StartingAbilities = new List<string>();
        }
    }
}