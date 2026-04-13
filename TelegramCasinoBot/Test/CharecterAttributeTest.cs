using System;
using TelegramCasinoBot.Models.Character;

namespace TelegramCasinoBot.Test
{
    internal class CharacterAttributeTest
    {
        static void Main()
        {
            Console.WriteLine("=== Тестирование CharacterAttribute ===\n");

            var health = new CharacterAttribute(100, 100);
            Console.WriteLine($"Создано: {health.Current}/{health.Max}");

            health.Add(-30);
            Console.WriteLine($"Урон -30: {health.Current}/{health.Max}");

            health.Add(20);
            Console.WriteLine($"Лечение +20: {health.Current}/{health.Max}");

            health.Max = 150;
            Console.WriteLine($"Новый максимум 150: {health.Current}/{health.Max}");

            health.Add(100);
            Console.WriteLine($"Попытка лечения +100: {health.Current}/{health.Max}");

            health.Add(-200);
            Console.WriteLine($"Урон -200 (не ниже 0): {health.Current}/{health.Max}");

            health.Current = 80;
            Console.WriteLine($"Установка Current = 80: {health.Current}/{health.Max}");

            health.Max = -10;
            Console.WriteLine($"Попытка установить Max = -10: {health.Max} (должен стать 0)");

            Console.WriteLine($"ToString(): {health.ToString()}");

            Console.WriteLine("\nТест завершён.");
        }
    }
}