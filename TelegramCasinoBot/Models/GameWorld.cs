using System.Collections.Generic;
using TelegramMetroidvaniaBot.Models;

namespace TelegramMetroidvaniaBot
{
    public class GameWorld
    {
        public Dictionary<string, GameLocation> Locations { get; } = new();
    }
}