using System;
using System.Text.Json.Serialization;

namespace TelegramCasinoBot.Models.Character
{
    public class CharacterAttribute
    {
        private int _current;
        private int _max;

        public int Current
        {
            get => _current;
            set
            {
                var old = _current;
                _current = Math.Clamp(value, 0, Max);
                if (old != _current)
                    System.Diagnostics.Debug.WriteLine($"Health.Current changed from {old} to {_current} (Max={Max})");
            }
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

        public override string ToString() => Current.ToString();

        public CharacterAttribute() { }

        [JsonConstructor]
        public CharacterAttribute(int current, int max)
        {
            Max = max;
            Current = current;
        }

        public void Add(int amount) => Current += amount;
    }
}