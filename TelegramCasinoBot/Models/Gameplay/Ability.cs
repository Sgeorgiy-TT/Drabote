using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TelegramCasinoBot.Models.Gameplay
{
    public class Ability
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public int Damage { get; set; }
        public int ManaCost { get; set; }
        public int StaminaCost { get; set; }
        public int Cooldown { get; set; }
        public string Target { get; set; }
        public string RequiredClass { get; set; }
        public string RequiredRace { get; set; }
        public string RequiredGender { get; set; }
        public string RequiredWeaponType { get; set; }
        public int IgnoreArmor { get; set; }
        public int MinLevel { get; set; }
        public double Probability { get; set; } = 1.0;
        public int? Price { get; set; }
        public bool IsSoldByTrader { get; set; } = false;
        public List<AbilityEffect> Effects { get; set; }
        public Ability() { }
    }

    public class AbilityEffect
    {
        public string Type { get; set; }
        public int Value { get; set; }
        public int Duration { get; set; }
        public bool Stackable { get; set; }
    }
}