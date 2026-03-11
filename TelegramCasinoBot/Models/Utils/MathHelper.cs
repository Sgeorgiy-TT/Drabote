using System;

namespace TelegramMetroidvaniaBot.Utils
{
    public static class MathHelper
    {
        /// <summary>
        /// Безопасное округление double до int с проверкой границ
        /// </summary>
        public static int SafeRound(double value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)Math.Round(value);
        }

        /// <summary>
        /// Безопасное преобразование double в int с проверкой границ
        /// </summary>
        public static int SafeToInt(double value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)value;
        }

        /// <summary>
        /// Вычисление урона с учетом защиты и модификаторов
        /// </summary>
        public static int CalculateDamage(int baseDamage, int defense, double damageMultiplier = 1.0)
        {
            // Защита снижает урон: урон = базовый_урон * множитель - защита
            var rawDamage = baseDamage * damageMultiplier;
            var finalDamage = rawDamage - defense;

            // Минимальный урон - 1
            return Math.Max(1, SafeRound(finalDamage));
        }

        /// <summary>
        /// Вычисление процента с округлением
        /// </summary>
        public static double CalculatePercentage(int value, int total)
        {
            if (total == 0) return 0;
            return Math.Round((double)value / total * 100, 1);
        }

        /// <summary>
        /// Применение процентного модификатора с округлением
        /// </summary>
        public static int ApplyPercentage(int baseValue, double percentage)
        {
            var result = baseValue * (1 + percentage / 100);
            return SafeRound(result);
        }

        /// <summary>
        /// Ограничение значения в пределах min-max
        /// </summary>
        public static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        /// <summary>
        /// Ограничение double значения
        /// </summary>
        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        /// <summary>
        /// Безопасное умножение с проверкой на переполнение
        /// </summary>
        public static int SafeMultiply(int a, double b)
        {
            var result = a * b;
            return SafeRound(result);
        }

        /// <summary>
        /// Восстановление характеристики со временем
        /// </summary>
        public static int CalculateRegeneration(int current, int max, int regenRate, TimeSpan timePassed)
        {
            var ticks = timePassed.TotalSeconds / 30; // Каждые 30 секунд
            var regenAmount = SafeRound(ticks * regenRate);
            return Clamp(current + regenAmount, 0, max);
        }
    }
}