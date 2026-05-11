using System;

namespace TelegramCasinoBot.Models.Gameplay
{
    public class ActiveEffect
    {
        public string Type { get; set; }
        public int Value { get; set; }
        public int RemainingDuration { get; set; }
        public bool Stackable { get; set; }

        public ActiveEffect(string type, int value, int duration, bool stackable = false)
        {
            Type = type;
            Value = value;
            RemainingDuration = duration;
            Stackable = stackable;
        }

        public void DecrementDuration() => RemainingDuration--;
        public bool IsExpired => RemainingDuration <= 0;
    }
}