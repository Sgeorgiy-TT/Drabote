using System.Collections.Generic;

namespace TelegramMetroidvaniaBot
{
    public class Location
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }
        public int Width { get; set; } = 10;
        public int Height { get; set; } = 10;

        public Dictionary<string, List<Position>> Objects { get; set; } = new Dictionary<string, List<Position>>();

        public List<LocationExit> Exits { get; set; } = new List<LocationExit>();

        public Location NorthLocation { get; set; }
        public Location SouthLocation { get; set; }
        public Location EastLocation { get; set; }
        public Location WestLocation { get; set; }

        public int WorldMapX { get; set; }
        public int WorldMapY { get; set; }

        public string RequiredAbility { get; set; }
        public string AccessDeniedMessage { get; set; }
        public Location(string id, string name, int width, int height, int worldMapX, int worldMapY)
        {
            Id = id;
            Name = name;
            Width = width;
            Height = height;
            WorldMapX = worldMapX;
            WorldMapY = worldMapY;
            Objects = new Dictionary<string, List<Position>>();
            Exits = new List<LocationExit>();
            Items = new List<string>();
        }
        public List<string> Items { get; set; } = new List<string>();
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
        public string TargetLocationId { get; set; }
        public Position Position { get; set; }
        public string Direction { get; set; }
        public string Description { get; set; }
        public LocationExit(string targetLocationId, Position position, string direction)
        {
            TargetLocationId = targetLocationId;
            Position = position;
            Direction = direction;
        }
        public string RequiredAbility { get; set; }
    }
}