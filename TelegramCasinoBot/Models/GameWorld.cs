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
            // Создаем расширенные локации 10x10
            var start = CreateStartLocation();
            var ancientTemple = CreateAncientTemple();
            var crystalCave = CreateCrystalCave();
            var forbiddenForest = CreateForbiddenForest();
            var bossChamber = CreateBossChamber();
            var finalSanctum = CreateFinalSanctum();

            // Устанавливаем связи между локациями
            SetupLocationConnections(start, ancientTemple, crystalCave, forbiddenForest, bossChamber, finalSanctum);

            // Добавляем в словарь
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
                new Position(5, 2) // Старый стражник
            },
                    ["obstacles"] = new List<Position>
            {
                // Границы локации (стены)
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                
                // Внутренние препятствия
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
            // Проход на СЕВЕР (в древний храм) - только в точке (5, 0)
            new LocationExit
            {
                TargetLocationId = "ancient_temple",
                Position = new Position(5, 0), // ВЕРХНИЙ ПРОХОД
                Direction = "north",
                Description = "Вы входите в древний храм..."
            },
            // Проход на ЮГ (в запретный лес) - только в точке (5, 9)
            new LocationExit
            {
                TargetLocationId = "forbidden_forest",
                Position = new Position(5, 9), // НИЖНИЙ ПРОХОД
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
                new Position(3, 2), // Древний артефакт
                new Position(6, 7)
            },
                    ["obstacles"] = new List<Position>
            {
                // Границы локации
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                
                // Колонны и алтари
                new Position(2, 3), new Position(2, 4), new Position(2, 5),
                new Position(7, 3), new Position(7, 4), new Position(7, 5),
                new Position(4, 2), new Position(5, 2)
            },
                    ["enemies"] = new List<Position>
            {
                new Position(4, 1), // Храмовый страж
                new Position(5, 8)  // Древний голем
            }
                },
                Exits = new List<LocationExit>
        {
            // Проход на ЮГ (обратно в руины)
            new LocationExit
            {
                TargetLocationId = "start",
                Position = new Position(5, 9),
                Direction = "south",
                Description = "Вы возвращаетесь к руинам..."
            },
            // Проход на СЕВЕР (в кристальную пещеру)
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
                new Position(2, 2), // Магический кристалл
                new Position(7, 7)  // Сундук с сокровищами
            },
                    ["obstacles"] = new List<Position>
            {
                // Границы локации
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                
                // Кристаллические образования
                new Position(1, 1), new Position(1, 2),
                new Position(8, 8), new Position(8, 7),
                new Position(3, 6), new Position(4, 6),
                new Position(6, 3), new Position(6, 4)
            },
                    ["special"] = new List<Position>
            {
                new Position(5, 5) // Магический кристалл для изучения способности
            }
                },
                Exits = new List<LocationExit>
        {
            // Проход на ЮГ (обратно в храм)
            new LocationExit
            {
                TargetLocationId = "ancient_temple",
                Position = new Position(5, 9),
                Direction = "south",
                Description = "Вы поднимаетесь обратно в храм..."
            },
            // Проход на ВОСТОК (в зал стражей)
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
                new Position(3, 7), // Ключ от ворот
                new Position(8, 2)  // Лесной артефакт
            },
                    ["npcs"] = new List<Position>
            {
                new Position(2, 3) // Лесной отшельник
            },
                    ["obstacles"] = new List<Position>
            {
                // Границы локации
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                
                // Деревья и заросли
                new Position(4, 4), new Position(4, 5), new Position(5, 4), new Position(5, 5),
                new Position(1, 8), new Position(2, 8),
                new Position(8, 6), new Position(8, 7),
                new Position(3, 2), new Position(7, 3)
            },
                    ["enemies"] = new List<Position>
            {
                new Position(6, 1),  // Лесной тролль
                new Position(3, 9),  // Ядовитое растение
                new Position(9, 4)   // Лесной дух
            }
                },
                Exits = new List<LocationExit>
        {
            // Проход на ВОСТОК (обратно в руины)
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
                new Position(5, 5) // Позиция босса
            },
                    ["obstacles"] = new List<Position>
            {
                // Границы локации
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                
                // Колонны по углам
                new Position(2, 2), new Position(2, 7),
                new Position(7, 2), new Position(7, 7)
            }
                },
                Exits = new List<LocationExit>
        {
            // Проход на ЗАПАД (обратно в пещеру)
            new LocationExit
            {
                TargetLocationId = "crystal_cave",
                Position = new Position(0, 5),
                Direction = "west",
                Description = "Вы отступаете в кристальную пещеру..."
            },
            // Проход на СЕВЕР (в святилище)
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
                new Position(5, 5) // Финальный сундук с наградой
            },
                    ["npcs"] = new List<Position>
            {
                new Position(2, 2), // Дух древнего
                new Position(7, 7)  // Хранитель знаний
            },
                    ["obstacles"] = new List<Position>
            {
                // Границы локации
                new Position(0, 0), new Position(1, 0), new Position(2, 0), new Position(3, 0), new Position(4, 0), new Position(5, 0), new Position(6, 0), new Position(7, 0), new Position(8, 0), new Position(9, 0),
                new Position(0, 9), new Position(1, 9), new Position(2, 9), new Position(3, 9), new Position(4, 9), new Position(5, 9), new Position(6, 9), new Position(7, 9), new Position(8, 9), new Position(9, 9),
                new Position(0, 1), new Position(0, 2), new Position(0, 3), new Position(0, 4), new Position(0, 5), new Position(0, 6), new Position(0, 7), new Position(0, 8),
                new Position(9, 1), new Position(9, 2), new Position(9, 3), new Position(9, 4), new Position(9, 5), new Position(9, 6), new Position(9, 7), new Position(9, 8),
                
                // Декоративные элементы
                new Position(3, 3), new Position(3, 6),
                new Position(6, 3), new Position(6, 6)
            }
                },
                Exits = new List<LocationExit>
        {
            // Проход на ЮГ (обратно в зал стражей)
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
            // Устанавливаем связи для карты мира
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