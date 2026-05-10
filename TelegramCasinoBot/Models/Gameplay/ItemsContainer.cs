using System.Collections.Generic;

namespace TelegramCasinoBot.Models.Gameplay
{
    internal class ItemsContainer
    {
        public List<Item> Weapons { get; set; }
        public List<Item> Armors { get; set; }
        public List<Item> Consumables { get; set; }
    }
}