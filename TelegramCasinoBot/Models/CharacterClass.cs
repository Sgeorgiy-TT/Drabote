using System.Collections.Generic;

namespace TelegramMetroidvaniaBot.Models
{
    public class CharacterClass : CharacterStats
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; set; }
        public string[] PreferredWeaponTypes { get; set; }

        public List<string> StartingAbilities { get; init; } = new List<string>();

        public CharacterClass(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}