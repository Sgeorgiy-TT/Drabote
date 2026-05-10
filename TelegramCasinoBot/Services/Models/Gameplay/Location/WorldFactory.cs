using System.Collections.Generic;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Models.Gameplay;

namespace TelegramCasinoBot.Services.Models.Gameplay.Location
{
    public class WorldFactory
    {
        public GameWorld CreateWorld()
        {
            var world = new GameWorld();

            var start = CreateStartLocation();
            var ancientTemple = CreateAncientTemple();
            var crystalCave = CreateCrystalCave();
            var forbiddenForest = CreateForbiddenForest();
            var bossChamber = CreateBossChamber();
            var finalSanctum = CreateFinalSanctum();

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

            world.Locations.Add(start.Id, start);
            world.Locations.Add(ancientTemple.Id, ancientTemple);
            world.Locations.Add(crystalCave.Id, crystalCave);
            world.Locations.Add(forbiddenForest.Id, forbiddenForest);
            world.Locations.Add(bossChamber.Id, bossChamber);
            world.Locations.Add(finalSanctum.Id, finalSanctum);

            return world;
        }

        private GameLocation CreateStartLocation()
        {
            return new GameLocation("start", "Вход в подземелье", 10, 10, 2, 4, level: 2)
            {
                Description = "Массивные каменные врата ведут в темноту. Своды покрыты древними рунами. Влажный воздух несёт запах плесени и сырости. Здесь начинается путь в недра земли.",
                ImagePath = "Assets/location_1.png",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position> { new Position(2, 3), new Position(7, 8) },
                    ["npcs"] = new List<Position> { new Position(5, 2) },
                    ["obstacles"] = new List<Position>
                    {
                        new Position(0,0), new Position(1,0), new Position(2,0), new Position(3,0), new Position(4,0), new Position(5,0), new Position(6,0), new Position(7,0), new Position(8,0), new Position(9,0),
                        new Position(0,9), new Position(1,9), new Position(2,9), new Position(3,9), new Position(4,9), new Position(5,9), new Position(6,9), new Position(7,9), new Position(8,9), new Position(9,9),
                        new Position(0,1), new Position(0,2), new Position(0,3), new Position(0,4), new Position(0,5), new Position(0,6), new Position(0,7), new Position(0,8),
                        new Position(9,1), new Position(9,2), new Position(9,3), new Position(9,4), new Position(9,5), new Position(9,6), new Position(9,7), new Position(9,8),
                        new Position(3,4), new Position(4,4), new Position(6,6), new Position(7,6),
                        new Position(2,7), new Position(3,7), new Position(1,3), new Position(8,2)
                    },
                    ["enemies"] = new List<Position> { new Position(1, 7), new Position(8, 1) }
                },
                Exits = new List<LocationExit>
                {
                    new LocationExit("ancient_temple", new Position(5, 0), "north") { Description = "Вы входите в Зал древних воинов..." },
                    new LocationExit("forbidden_forest", new Position(5, 9), "south") { Description = "Вы углубляетесь в Лес теней..." }
                }
            };
        }

        private GameLocation CreateAncientTemple()
        {
            return new GameLocation("ancient_temple", "Зал древних воинов", 10, 10, 2, 3, level: 4)
            {
                Description = "Огромный зал с колоннами. Вдоль стен выстроились каменные статуи павших героев. На полу видны следы былых сражений. В северной части мерцает проход в кристальный грот.",
                ImagePath = "Assets/hram.jpn.png",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position> { new Position(3, 2), new Position(6, 7) },
                    ["obstacles"] = new List<Position>
                    {
                        new Position(0,0), new Position(1,0), new Position(2,0), new Position(3,0), new Position(4,0), new Position(5,0), new Position(6,0), new Position(7,0), new Position(8,0), new Position(9,0),
                        new Position(0,9), new Position(1,9), new Position(2,9), new Position(3,9), new Position(4,9), new Position(5,9), new Position(6,9), new Position(7,9), new Position(8,9), new Position(9,9),
                        new Position(0,1), new Position(0,2), new Position(0,3), new Position(0,4), new Position(0,5), new Position(0,6), new Position(0,7), new Position(0,8),
                        new Position(9,1), new Position(9,2), new Position(9,3), new Position(9,4), new Position(9,5), new Position(9,6), new Position(9,7), new Position(9,8),
                        new Position(2,3), new Position(2,4), new Position(2,5),
                        new Position(7,3), new Position(7,4), new Position(7,5),
                        new Position(4,2), new Position(5,2)
                    },
                    ["enemies"] = new List<Position> { new Position(4, 1), new Position(5, 8) }
                },
                Exits = new List<LocationExit>
                {
                    new LocationExit("start", new Position(5, 9), "south") { Description = "Вы возвращаетесь ко Входу в подземелье..." },
                    new LocationExit("crystal_cave", new Position(5, 0), "north") { Description = "Вы спускаетесь в Грот кристаллов...", RequiredAbility = "Двойной прыжок" }
                }
            };
        }

        private GameLocation CreateCrystalCave()
        {
            return new GameLocation("crystal_cave", "Грот кристаллов", 10, 10, 2, 2, level: 7)
            {
                Description = "Пещера, стены которой усеяны огромными светящимися кристаллами. Они мерцают в темноте, наполняя воздух магией. В глубине слышен гул неведомой силы.",
                RequiredAbility = "Двойной прыжок",
                AccessDeniedMessage = "Нужно уметь прыгать выше, чтобы добраться до грота!",
                ImagePath = "Assets/pekera.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position> { new Position(2, 2), new Position(7, 7) },
                    ["obstacles"] = new List<Position>
                    {
                        new Position(0,0), new Position(1,0), new Position(2,0), new Position(3,0), new Position(4,0), new Position(5,0), new Position(6,0), new Position(7,0), new Position(8,0), new Position(9,0),
                        new Position(0,9), new Position(1,9), new Position(2,9), new Position(3,9), new Position(4,9), new Position(5,9), new Position(6,9), new Position(7,9), new Position(8,9), new Position(9,9),
                        new Position(0,1), new Position(0,2), new Position(0,3), new Position(0,4), new Position(0,5), new Position(0,6), new Position(0,7), new Position(0,8),
                        new Position(9,1), new Position(9,2), new Position(9,3), new Position(9,4), new Position(9,5), new Position(9,6), new Position(9,7), new Position(9,8),
                        new Position(1,1), new Position(1,2),
                        new Position(8,8), new Position(8,7),
                        new Position(3,6), new Position(4,6),
                        new Position(6,3), new Position(6,4)
                    },
                    ["special"] = new List<Position> { new Position(5, 5) }
                },
                Exits = new List<LocationExit>
                {
                    new LocationExit("ancient_temple", new Position(5, 9), "south") { Description = "Вы поднимаетесь обратно в Зал древних воинов..." },
                    new LocationExit("boss_chamber", new Position(9, 5), "east") { Description = "Вы входите в Тронный зал стража...", RequiredAbility = "Лазерный луч" }
                }
            };
        }

        private GameLocation CreateForbiddenForest()
        {
            return new GameLocation("forbidden_forest", "Лес теней", 10, 10, 1, 4, level: 5)
            {
                Description = "Странный лес, растущий под землёй. Грибы высотой с дерево, светящиеся лианы, чьи-то глаза в темноте. Тишину нарушает только капель с потолка.",
                ImagePath = "Assets/les.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position> { new Position(3, 7), new Position(8, 2) },
                    ["npcs"] = new List<Position> { new Position(2, 3) },
                    ["obstacles"] = new List<Position>
                    {
                        new Position(0,0), new Position(1,0), new Position(2,0), new Position(3,0), new Position(4,0), new Position(5,0), new Position(6,0), new Position(7,0), new Position(8,0), new Position(9,0),
                        new Position(0,9), new Position(1,9), new Position(2,9), new Position(3,9), new Position(4,9), new Position(5,9), new Position(6,9), new Position(7,9), new Position(8,9), new Position(9,9),
                        new Position(0,1), new Position(0,2), new Position(0,3), new Position(0,4), new Position(0,5), new Position(0,6), new Position(0,7), new Position(0,8),
                        new Position(9,1), new Position(9,2), new Position(9,3), new Position(9,4), new Position(9,5), new Position(9,6), new Position(9,7), new Position(9,8),
                        new Position(4,4), new Position(4,5), new Position(5,4), new Position(5,5),
                        new Position(1,8), new Position(2,8),
                        new Position(8,6), new Position(8,7),
                        new Position(3,2), new Position(7,3)
                    },
                    ["enemies"] = new List<Position> { new Position(6, 1), new Position(3, 9), new Position(9, 4) }
                },
                Exits = new List<LocationExit>
                {
                    new LocationExit("start", new Position(9, 5), "east") { Description = "Вы возвращаетесь ко Входу в подземелье..." }
                }
            };
        }

        private GameLocation CreateBossChamber()
        {
            return new GameLocation("boss_chamber", "Тронный зал стража", 10, 10, 3, 2, level: 12)
            {
                Description = "Грандиозный зал с высоким потолком. В центре возвышается трон, на котором восседает древний каменный страж — хранитель глубин. Его глаза светятся магическим огнём.",
                RequiredAbility = "Лазерный луч",
                AccessDeniedMessage = "Нужно больше мощи, чтобы сразить стража!",
                ImagePath = "Assets/zalstr.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["boss"] = new List<Position> { new Position(5, 5) },
                    ["obstacles"] = new List<Position>
                    {
                        new Position(0,0), new Position(1,0), new Position(2,0), new Position(3,0), new Position(4,0), new Position(5,0), new Position(6,0), new Position(7,0), new Position(8,0), new Position(9,0),
                        new Position(0,9), new Position(1,9), new Position(2,9), new Position(3,9), new Position(4,9), new Position(5,9), new Position(6,9), new Position(7,9), new Position(8,9), new Position(9,9),
                        new Position(0,1), new Position(0,2), new Position(0,3), new Position(0,4), new Position(0,5), new Position(0,6), new Position(0,7), new Position(0,8),
                        new Position(9,1), new Position(9,2), new Position(9,3), new Position(9,4), new Position(9,5), new Position(9,6), new Position(9,7), new Position(9,8),
                        new Position(2,2), new Position(2,7),
                        new Position(7,2), new Position(7,7)
                    }
                },
                Exits = new List<LocationExit>
                {
                    new LocationExit("crystal_cave", new Position(0, 5), "west") { Description = "Вы отступаете в Грот кристаллов..." },
                    new LocationExit("final_sanctum", new Position(5, 0), "north") { Description = "Врата открываются! Вы входите в Сокровищницу...", RequiredAbility = "Открытие ворот" }
                }
            };
        }

        private GameLocation CreateFinalSanctum()
        {
            return new GameLocation("final_sanctum", "Сокровищница", 10, 10, 3, 1, level: 15)
            {
                Description = "Комната, полная золота и артефактов. В центре стоит огромный сундук, украшенный драгоценными камнями. В дальней стене виднеется запечатанный портал, ведущий глубже в подземелье.",
                RequiredAbility = "Открытие ворот",
                ImagePath = "Assets/swat.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position> { new Position(5, 5) },
                    ["npcs"] = new List<Position> { new Position(2, 2), new Position(7, 7) },
                    ["obstacles"] = new List<Position>
                    {
                        new Position(0,0), new Position(1,0), new Position(2,0), new Position(3,0), new Position(4,0), new Position(5,0), new Position(6,0), new Position(7,0), new Position(8,0), new Position(9,0),
                        new Position(0,9), new Position(1,9), new Position(2,9), new Position(3,9), new Position(4,9), new Position(5,9), new Position(6,9), new Position(7,9), new Position(8,9), new Position(9,9),
                        new Position(0,1), new Position(0,2), new Position(0,3), new Position(0,4), new Position(0,5), new Position(0,6), new Position(0,7), new Position(0,8),
                        new Position(9,1), new Position(9,2), new Position(9,3), new Position(9,4), new Position(9,5), new Position(9,6), new Position(9,7), new Position(9,8),
                        new Position(3,3), new Position(3,6),
                        new Position(6,3), new Position(6,6)
                    }
                },
                Exits = new List<LocationExit>
                {
                    new LocationExit("boss_chamber", new Position(5, 9), "south") { Description = "Вы возвращаетесь в Тронный зал стража..." }
                }
            };
        }
    }
}