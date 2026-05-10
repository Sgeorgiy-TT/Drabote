using System.Collections.Generic;

namespace TelegramCasinoBot.Models.Gameplay
{
    public class Mob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }
        public int Level { get; set; }
        public int Health { get; set; }
        public int Mana { get; set; }
        public int Stamina { get; set; }
        public int Defense { get; set; }
        public int ExperienceReward { get; set; }
        public int GoldReward { get; set; }
        public DropTable DropTable { get; set; }
        public int DamageMin { get; set; } = 5;
        public int DamageMax { get; set; } = 15;
    }

    public class DropTable
    {
        public List<DropItem> Items { get; set; }
    }

    public class DropItem
    {
        public int ItemId { get; set; }
        public double Chance { get; set; }
    }
}