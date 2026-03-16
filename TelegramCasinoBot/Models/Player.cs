using System.Collections.Generic;

namespace TelegramMetroidvaniaBot
{
    public class Player
    {
        public long ChatId { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Race { get; set; }
        public string Class { get; set; }
        public string IconPath { get; set; }

        public string CurrentLocation { get; set; }
        public int PositionX { get; set; } = 5; 
        public int PositionY { get; set; } = 5;

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

        public List<string> Inventory { get; set; } = new List<string>();
        public List<string> Abilities { get; set; } = new List<string>();
        public List<string> QuestCompleted { get; set; } = new List<string>();

        public int BossHealth { get; set; }
        public int LastBossMessageId { get; set; }
        public int LastMessageId { get; set; }

        public double ExperienceMultiplier { get; set; } = 1.0;
        public double MeleeDamageMultiplier { get; set; } = 1.0;
        public double RangedDamageMultiplier { get; set; } = 1.0;
        public double MagicDamageMultiplier { get; set; } = 1.0;

        public Dictionary<string, List<Position>> ExploredAreas { get; set; } = new Dictionary<string, List<Position>>();

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