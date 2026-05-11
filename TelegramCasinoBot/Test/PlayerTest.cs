using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.JsonR;
using TelegramCasinoBot.Services.Data;

namespace TelegramCasinoBot.Test
{
    internal class PlayerTest
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Тестирование Player (загрузка из JSON)\n");

            var baseDir = Directory.GetCurrentDirectory();
            var racesPath = Path.Combine(baseDir, "Assets", "Data", "Races.json");
            var classesPath = Path.Combine(baseDir, "Assets", "Data", "Classes.json");

            if (!File.Exists(racesPath) || !File.Exists(classesPath))
            {
                Console.WriteLine("Файлы JSON не найдены. Убедитесь, что они скопированы в выходную папку.");
                return;
            }

            var raceRepo = new JsonRaceRepository(NullLogger<JsonRaceRepository>.Instance);
            var classRepo = new JsonClassRepository(NullLogger<JsonClassRepository>.Instance);
            var races = raceRepo.GetAllRacesAsync().GetAwaiter().GetResult();
            Console.WriteLine($"Загружено рас: {races.Count}");
            foreach (var r in races) Console.WriteLine($" - {r.Name}");

            var classes = classRepo.GetAllClassesAsync().GetAwaiter().GetResult();
            Console.WriteLine($"Загружено классов: {classes.Count}");
            foreach (var c in classes) Console.WriteLine($" - {c.Name}");
            var race = raceRepo.GetRaceByNameAsync("Человек").GetAwaiter().GetResult();
            var playerClass = classRepo.GetClassByNameAsync("Воин").GetAwaiter().GetResult();

            if (race == null || playerClass == null)
            {
                Console.WriteLine("Не удалось загрузить расу или класс из JSON.");
                return;
            }

            var player = new Player(12345, "Тестер", "Male", race, playerClass, "icon.png");

            Console.WriteLine("=== Создание персонажа ===");
            Console.WriteLine($"Имя: {player.Name}");
            Console.WriteLine($"Пол: {player.Gender}");
            Console.WriteLine($"Раса: {player.Race}");
            Console.WriteLine($"Класс: {player.Class}");
            Console.WriteLine($"Иконка: {(string.IsNullOrEmpty(player.IconPath) ? "не задана" : player.IconPath)}");
            PrintStats(player);

            Console.WriteLine("\n=== Добавление опыта ===");
            //player.AddExperience(150);
            PrintStats(player);

            Console.WriteLine("\n=== Урон и лечение ===");
            player.Health.Add(-30);
            Console.WriteLine($"После урона 30: {player.Health.Current}/{player.Health.Max}");
            player.Health.Add(20);
            Console.WriteLine($"После лечения 20: {player.Health.Current}/{player.Health.Max}");

            Console.WriteLine("\nТестирование завершено.");
        }

        static void PrintStats(Player p)
        {
            Console.WriteLine($"Уровень: {p.Level}, Опыт: {p.Experience}");
            Console.WriteLine($"Здоровье: {p.Health.Current}/{p.Health.Max}");
            Console.WriteLine($"Мана: {p.Mana.Current}/{p.Mana.Max}");
            Console.WriteLine($"Выносливость: {p.Stamina.Current}/{p.Stamina.Max}");
            Console.WriteLine($"Защита: {p.Defense}");
        }
    }
}