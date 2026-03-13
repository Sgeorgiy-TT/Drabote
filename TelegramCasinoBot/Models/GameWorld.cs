using System.Collections.Generic;
namespace TelegramMetroidvaniaBot
{
    public class GameWorld
    {
        public Dictionary<string, Location> Locations { get; set; } = new Dictionary<string, Location>();
        public GameWorld()
        {
            InitializeWorld();
        }
        private void InitializeWorld()
        {
            var start = CreateStartLocation();
            var ancientTemple = CreateAncientTemple();
            var crystalCave = CreateCrystalCave();
            var forbiddenForest = CreateForbiddenForest();
            var bossChamber = CreateBossChamber();
            var finalSanctum = CreateFinalSanctum();
            SetupLocationConnections(start, ancientTemple, crystalCave, forbiddenForest, bossChamber, finalSanctum);
            AddLocationsToDictionary(start, ancientTemple, crystalCave, forbiddenForest, bossChamber, finalSanctum);
        }
        private Location CreateStartLocation()
        {
            return new Location
            {
                Id = "start",
                Name = "Забытые Руины",
                Description = "Обширная территория древних руин. Камни покрыты мхом, воздух наполнен тайной. На севере виднеется вход в древний храм, на юге - тропа в запретный лес.",
                Width = 10,
                Height = 10,
                WorldMapX = 2,
                WorldMapY = 4,
                ImagePath = "Assets/location_1.png",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position>
            {
                new Position(2, 3),
                new Position(7, 8)
            },
                    ["npcs"] = new List<Position>
            {
                new Position(5, 2)
            },
                    ["obstacles"] = new List<Position>
            {
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                new Position(3, 4), new Position(4, 4), new Position(6, 6), new Position(7, 6),
                new Position(2, 7), new Position(3, 7), new Position(1, 3), new Position(8, 2)
            },
                    ["enemies"] = new List<Position>
            {
                new Position(1, 7),
                new Position(8, 1)
            }
                },
                Exits = new List<LocationExit>
        {
            new LocationExit
            {
                TargetLocationId = "ancient_temple",
                Position = new Position(5, 0),
                Direction = "north",
                Description = "Вы входите в древний храм..."
            },
            new LocationExit
            {
                TargetLocationId = "forbidden_forest",
                Position = new Position(5, 9),
                Direction = "south",
                Description = "Вы углубляетесь в запретный лес..."
            }
        }
            };
        }
        private Location CreateAncientTemple()
        {
            return new Location
            {
                Id = "ancient_temple",
                Name = "Древний Храм",
                Description = "Огромный храм с высокими потолками. На стенах древние фрески, изображающие forgotten цивилизации. " +
                             "В северной части виднеется проход в пещеру.",
                Width = 10,
                Height = 10,
                WorldMapX = 2,
                WorldMapY = 3,
                ImagePath = "Assets/hram.jpn.png",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position>
            {
                new Position(3, 2),
                new Position(6, 7)
            },
                    ["obstacles"] = new List<Position>
            {
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                new Position(2, 3), new Position(2, 4), new Position(2, 5),
                new Position(7, 3), new Position(7, 4), new Position(7, 5),
                new Position(4, 2), new Position(5, 2)
            },
                    ["enemies"] = new List<Position>
            {
                new Position(4, 1), 
                new Position(5, 8) 
            }
                },
                Exits = new List<LocationExit>
        {
            new LocationExit
            {
                TargetLocationId = "start",
                Position = new Position(5, 9),
                Direction = "south",
                Description = "Вы возвращаетесь к руинам..."
            },
            new LocationExit
            {
                TargetLocationId = "crystal_cave",
                Position = new Position(5, 0),
                Direction = "north",
                Description = "Вы спускаетесь в кристальную пещеру...",
                RequiredAbility = "Двойной прыжок"
            }
        }
            };
        }
        private Location CreateCrystalCave()
        {
            return new Location
            {
                Id = "crystal_cave",
                Name = "Кристальная Пещера",
                Description = "Пещера, сияющая разноцветными кристаллами. Они излучают магическую энергию, " +
                             "наполняя воздух электрическим трепетом. На востоке виднеется большой зал.",
                Width = 10,
                Height = 10,
                WorldMapX = 2,
                WorldMapY = 2,
                RequiredAbility = "Двойной прыжок",
                AccessDeniedMessage = "Нужно уметь прыгать выше, чтобы добраться до пещеры!",
                ImagePath = "Assets/pekera.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position>
            {
                new Position(2, 2),
                new Position(7, 7) 
            },
                    ["obstacles"] = new List<Position>
            {
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                new Position(1, 1), new Position(1, 2),
                new Position(8, 8), new Position(8, 7),
                new Position(3, 6), new Position(4, 6),
                new Position(6, 3), new Position(6, 4)
            },
                    ["special"] = new List<Position>
            {
                new Position(5, 5) 
            }
                },
                Exits = new List<LocationExit>
        {
            new LocationExit
            {
                TargetLocationId = "ancient_temple",
                Position = new Position(5, 9),
                Direction = "south",
                Description = "Вы поднимаетесь обратно в храм..."
            },
            new LocationExit
            {
                TargetLocationId = "boss_chamber",
                Position = new Position(9, 5),
                Direction = "east",
                Description = "Вы входите в зал стражей...",
                RequiredAbility = "Лазерный луч"
            }
        }
            };
        }
        private Location CreateForbiddenForest()
        {
            return new Location
            {
                Id = "forbidden_forest",
                Name = "Запретный Лес",
                Description = "Густой мистический лес с гигантскими грибами и twisted деревьями. " +
                             "Воздух мерцает магией, а в глубине слышны странные звуки.",
                Width = 10,
                Height = 10,
                WorldMapX = 1,
                WorldMapY = 4,
                ImagePath = "Assets/les.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position>
            {
                new Position(3, 7),
                new Position(8, 2) 
            },
                    ["npcs"] = new List<Position>
            {
                new Position(2, 3) 
            },
                    ["obstacles"] = new List<Position>
            {
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                new Position(4, 4), new Position(4, 5), new Position(5, 4), new Position(5, 5),
                new Position(1, 8), new Position(2, 8),
                new Position(8, 6), new Position(8, 7),
                new Position(3, 2), new Position(7, 3)
            },
                    ["enemies"] = new List<Position>
            {
                new Position(6, 1),
                new Position(3, 9),  
                new Position(9, 4) 
            }
                },
                Exits = new List<LocationExit>
        {
            new LocationExit
            {
                TargetLocationId = "start",
                Position = new Position(9, 5),
                Direction = "east",
                Description = "Вы возвращаетесь к руинам..."
            }
        }
            };
        }
        private Location CreateBossChamber()
        {
            return new Location
            {
                Id = "boss_chamber",
                Name = "Зал Стражей",
                Description = "Огромный зал с массивными вратами. В центре стоит древний страж, " +
                             "защищающий проход в святилище. На севере виднеются врата.",
                Width = 10,
                Height = 10,
                WorldMapX = 3,
                WorldMapY = 2,
                RequiredAbility = "Лазерный луч",
                AccessDeniedMessage = "Страж слишком силен! Нужно больше мощи!",
                ImagePath = "Assets/zalstr.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["boss"] = new List<Position>
            {
                new Position(5, 5) 
            },
                    ["obstacles"] = new List<Position>
            {
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                new Position(2, 2), new Position(2, 7),
                new Position(7, 2), new Position(7, 7)
            }
                },
                Exits = new List<LocationExit>
        {
            new LocationExit
            {
                TargetLocationId = "crystal_cave",
                Position = new Position(0, 5),
                Direction = "west",
                Description = "Вы отступаете в кристальную пещеру..."
            },
            new LocationExit
            {
                TargetLocationId = "final_sanctum",
                Position = new Position(5, 0),
                Direction = "north",
                Description = "Врата открываются! Вы входите в святилище...",
                RequiredAbility = "Открытие ворот"
            }
        }
            };
        }
        private Location CreateFinalSanctum()
        {
            return new Location
            {
                Id = "final_sanctum",
                Name = "Святилище Древних",
                Description = "Вы достигли цели! Святилище наполнено ярким светом и древней мудростью. " +
                             "Здесь хранятся величайшие тайны Аркадии!",
                Width = 10,
                Height = 10,
                WorldMapX = 3,
                WorldMapY = 1,
                RequiredAbility = "Открытие ворот",
                ImagePath = "Assets/swat.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position>
            {
                new Position(5, 5)
            },
                    ["npcs"] = new List<Position>
            {
                new Position(2, 2),
                new Position(7, 7)  
            },
                    ["obstacles"] = new List<Position>
            {
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                new Position(3, 3), new Position(3, 6),
                new Position(6, 3), new Position(6, 6)
            }
                },
                Exits = new List<LocationExit>
        {
            new LocationExit
            {
                TargetLocationId = "boss_chamber",
                Position = new Position(5, 9),
                Direction = "south",
                Description = "Вы возвращаетесь в зал стражей..."
            }
        }
            };
        }
        private void SetupLocationConnections(Location start, Location ancientTemple, Location crystalCave,
                                            Location forbiddenForest, Location bossChamber, Location finalSanctum)
        {
            start.EastLocation = ancientTemple;
            start.WestLocation = forbiddenForest;
            ancientTemple.WestLocation = start;
            ancientTemple.NorthLocation = crystalCave;
            crystalCave.SouthLocation = ancientTemple;
            crystalCave.EastLocation = bossChamber;
            forbiddenForest.EastLocation = start;
            bossChamber.WestLocation = crystalCave;
            bossChamber.NorthLocation = finalSanctum;
            finalSanctum.SouthLocation = bossChamber;
        }
        private void AddLocationsToDictionary(params Location[] locations)
        {
            foreach (var location in locations)
            {
                Locations.Add(location.Id, location);
            }
        }
    }
}