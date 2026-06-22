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
                Description = "Открываеться вид на зеленую поляну со старыми постройками и деревьями.",
                ImagePath = "Assets/Location/location_1.png",
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
                    new LocationExit("forbidden_forest", new Position(0, 5), "west") { Description = "Вы углубляетесь в Лес..." }
                }
            };
        }

        private GameLocation CreateAncientTemple()
        {
            return new GameLocation("ancient_temple", "Зал древних воинов", 10, 10, 2, 3, level: 4)
            {
                Description = "Огромный зал с колоннами. Вдоль стен выстроились каменные статуи павших героев. На полу видны следы былых сражений. В северной части мерцает проход в кристальный грот.",
                ImagePath = "Assets/Location/hram.jpn.png",
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
                    ["enemies"] = new List<Position> { new Position(4, 1), new Position(5, 8) },
                    ["double_jump_key"] = new List<Position> { new Position(5, 5) }
                },
                Exits = new List<LocationExit>
                {
                    new LocationExit("start", new Position(5, 9), "south") { Description = "Вы возвращаетесь ко Входу в подземелье..." },
                    new LocationExit("crystal_cave", new Position(5, 0), "north") { Description = "Вы спускаетесь в Пещеру...", RequiredAbility = "Двойной прыжок" }
                }
            };
        }

        private GameLocation CreateCrystalCave()
        {
            return new GameLocation("crystal_cave", "Пещера", 10, 10, 2, 2, level: 7)
            {
                Description = "Пещера, стены которой усеяны мхом и сырастью. Мох мерцает в темноте, наполняя воздух магией. В глубине слышен гул неведомой силы.",
                RequiredAbility = "Двойной прыжок",
                AccessDeniedMessage = "Нужно уметь прыгать выше, чтобы добраться до грота!",
                ImagePath = "Assets/Location/pekera.jpg",
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
                        new Position(6,3), new Position(6,4), new Position(5,5)
                    }
                    
                },
                Exits = new List<LocationExit>
                {
                    new LocationExit("ancient_temple", new Position(5, 9), "south") { Description = "Вы поднимаетесь обратно в Зал древних воинов..." },
                    new LocationExit("boss_chamber", new Position(9, 5), "east") { Description = "Вы входите в Обитель волков..."}
                }
            };
        }

        private GameLocation CreateForbiddenForest()
        {
            return new GameLocation("forbidden_forest", "Лес", 10, 10, 1, 4, level: 5)
            {
                Description = "Странный лес, растущий под землёй. Грибы высотой с дерево, светящиеся лианы, чьи-то глаза в темноте. Тишину нарушает только капель с потолка.",
                ImagePath = "Assets/Location/les.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position> { new Position(3, 7), new Position(8, 2) },
                    
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
            return new GameLocation("boss_chamber", "Обитель волков", 10, 10, 3, 2, level: 12)
            {
                Description = "Вас встречает старый мощеный каменый пол заросший корнями. В этой местности живут пещерные волки, они сильней волков снаруже подземелья",
               
                ImagePath = "Assets/Location/zalstr.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    
                    ["obstacles"] = new List<Position>
                    {
                        new Position(0,0), new Position(1,0), new Position(2,0), new Position(3,0), new Position(4,0), new Position(5,0), new Position(6,0), new Position(7,0), new Position(8,0), new Position(9,0),
                        new Position(0,9), new Position(1,9), new Position(2,9), new Position(3,9), new Position(4,9), new Position(5,9), new Position(6,9), new Position(7,9), new Position(8,9), new Position(9,9),
                        new Position(0,1), new Position(0,2), new Position(0,3), new Position(0,4), new Position(0,5), new Position(0,6), new Position(0,7), new Position(0,8),
                        new Position(9,1), new Position(9,2), new Position(9,3), new Position(9,4), new Position(9,5), new Position(9,6), new Position(9,7), new Position(9,8),
                        new Position(2,2), new Position(2,7),
                        new Position(7,2), new Position(7,7), new Position(5,5)
                    }
                },
                Exits = new List<LocationExit>
                {
                    new LocationExit("crystal_cave", new Position(0, 5), "west") { Description = "Вы отступаете в Пещеру..." },
                    new LocationExit("final_sanctum", new Position(5, 0), "north") { Description = "Врата открываются! Вы входите в Сокровищницу..." }
                }
            };
        }

        private GameLocation CreateFinalSanctum()
        {
            return new GameLocation("final_sanctum", "Стариный зал", 10, 10, 3, 1, level: 15)
            {
                Description = "Стариный зал на открытом пространстве в конце которого виднеется проход в Глубины, но чтоб попасть в них, нужно сначало убить босса этого места. Владея ранее полученой информаицей, вам известно что в зале есть несколько големов, хозяев этого места, и чтобы пройти дальше вам не надо сражаться со всеми ними, а достаточно убить одного из них и достав из него ядро что отопред двери в Глубины.",
               
                ImagePath = "Assets/Location/swat.jpg",
                Objects = new Dictionary<string, List<Position>>
                {
                    ["chests"] = new List<Position> { new Position(5, 5) },
                    
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
                    new LocationExit("boss_chamber", new Position(5, 9), "south") { Description = "Вы возвращаетесь в Обитель волков..." }
                }
            };
        }
    }
}