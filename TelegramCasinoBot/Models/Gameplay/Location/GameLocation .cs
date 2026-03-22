using System.Collections.Generic;

namespace TelegramCasinoBot.Models.Gameplay.Location
{
    public class GameLocation
    {
        public string Id { get; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public GameLocation NorthLocation { get; set; }
        public GameLocation SouthLocation { get; set; }
        public GameLocation EastLocation { get; set; }
        public GameLocation WestLocation { get; set; }
        public int WorldMapX { get; set; }
        public int WorldMapY { get; set; }
        public string RequiredAbility { get; set; }
        public string AccessDeniedMessage { get; set; }
        public Dictionary<string, List<Position>> Objects { get; init; } = new Dictionary<string, List<Position>>();
        public List<LocationExit> Exits { get; init; } = new List<LocationExit>();
        public List<string> Items { get; init; } = new List<string>();

        public GameLocation(string id, string name, int width, int height, int worldMapX, int worldMapY)
        {
            Id = id;
            Name = name;
            Width = width;
            Height = height;
            WorldMapX = worldMapX;
            WorldMapY = worldMapY;
        }
    }

    public class Position
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class LocationExit
    {
        public string TargetLocationId { get; }
        public Position Position { get; }
        public string Direction { get; }
        public string Description { get; set; }
        public string RequiredAbility { get; set; }

        public LocationExit(string targetLocationId, Position position, string direction)
        {
            TargetLocationId = targetLocationId;
            Position = position;
            Direction = direction;
        }
    }
}