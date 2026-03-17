using System;

namespace TelegramCasinoBot.Utils
{
    public static class MathHelper
    {
        public static int SafeRound(double value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)Math.Round(value);
        }

        public static int SafeToInt(double value)
        {
            if (value > int.MaxValue) return int.MaxValue;
            if (value < int.MinValue) return int.MinValue;
            return (int)value;
        }

        public static int CalculateDamage(int baseDamage, int defense, double damageMultiplier = 1.0)
        {
            var rawDamage = baseDamage * damageMultiplier;
            var finalDamage = rawDamage - defense;

            return Math.Max(1, SafeRound(finalDamage));
        }

        public static double CalculatePercentage(int value, int total)
        {
            if (total == 0) return 0;
            return Math.Round((double)value / total * 100, 1);
        }

        public static int ApplyPercentage(int baseValue, double percentage)
        {
            var result = baseValue * (1 + percentage / 100);
            return SafeRound(result);
        }

        public static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static int SafeMultiply(int a, double b)
        {
            var result = a * b;
            return SafeRound(result);
        }

        public static int CalculateRegeneration(int current, int max, int regenRate, TimeSpan timePassed)
        {
            var ticks = timePassed.TotalSeconds / 30;
            var regenAmount = SafeRound(ticks * regenRate);
            return Clamp(current + regenAmount, 0, max);
        }
    }
}