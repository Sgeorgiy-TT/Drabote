using System.Collections.Generic;
using TelegramCasinoBot.Models.Character;

namespace TelegramCasinoBot.Models.Stats
{
    public class Class : CharacterStats
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; set; }
        public string[] PreferredWeaponTypes { get; set; }

        public List<string> StartingAbilities { get; init; } = new List<string>();

        public Class(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}