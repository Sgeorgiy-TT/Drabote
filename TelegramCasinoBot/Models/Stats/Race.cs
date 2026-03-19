using System.Collections.Generic;
using TelegramCasinoBot.Models.Character;

namespace TelegramCasinoBot.Models.Stats
{
    public class Race : CharacterStats
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; set; }
        public string[] AvailableGenders { get; set; }

        public List<string> SpecialAbilities { get; init; } = new List<string>();

        public Race(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}