using System.Collections.Generic;
using TelegramCasinoBot.Models.Gameplay;

namespace TelegramCasinoBot.Models.Gameplay.Location
{
    public class GameWorld
    {
        public Dictionary<string, GameLocation> Locations { get; } = new();
    }
}