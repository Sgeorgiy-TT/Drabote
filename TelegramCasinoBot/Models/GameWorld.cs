using System.Collections.Generic;

namespace TelegramMetroidvaniaBot
{
    public class GameWorld
    {
        public Dictionary<string, Location> Locations { get; } = new Dictionary<string, Location>();

        public GameWorld()
        {
            
        }
    }
}