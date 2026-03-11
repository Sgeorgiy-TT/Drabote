using System.Collections.Generic;

namespace TelegramMetroidvaniaBot
{
    public class Location
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; } // Путь к изображению локации
        public int Width { get; set; } = 10; // Ширина локации в клетках
        public int Height { get; set; } = 10; // Высота локации в клетках

        // Позиции объектов в локации (координаты x,y)
        public Dictionary<string, List<Position>> Objects { get; set; } = new Dictionary<string, List<Position>>();

        // Проходы в другие локации (позиция и направление)
        public List<LocationExit> Exits { get; set; } = new List<LocationExit>();

        // Соседние локации (для карты мира)
        public Location NorthLocation { get; set; }
        public Location SouthLocation { get; set; }
        public Location EastLocation { get; set; }
        public Location WestLocation { get; set; }

        // Координаты на карте мира
        public int WorldMapX { get; set; }
        public int WorldMapY { get; set; }

        // Требования для входа
        public string RequiredAbility { get; set; }
        public string AccessDeniedMessage { get; set; }

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
        public string Direction { get; set; } // "north", "south", "east", "west"
        public string Description { get; set; }

        public string RequiredAbility { get; set; }
    }
}