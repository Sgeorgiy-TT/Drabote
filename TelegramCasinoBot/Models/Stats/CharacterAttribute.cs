using System;

namespace TelegramCasinoBot.Models.Character
{
    public class CharacterAttribute
    {
        private int _current;
        private int _max;

        public int Current
        {
            get => _current;
            set => _current = Math.Clamp(value, 0, Max);
        }

        public int Max
        {
            get => _max;
            set
            {
                _max = Math.Max(0, value);
                _current = Math.Min(_current, _max);
            }
        }

        public CharacterAttribute(int current, int max)
        {
            Max = max;
            Current = current;
        }

        public void Add(int amount)
        {
            Current += amount;
        }

        public void Subtract(int amount)
        {
            Current -= amount;
        }
    }
}