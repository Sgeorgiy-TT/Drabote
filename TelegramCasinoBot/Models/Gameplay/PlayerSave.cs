using System;

namespace TelegramCasinoBot.Models.Gameplay
{
    [Deprecated]
    public class PlayerSave
    {
        public long ChatId { get; }
        public string PlayerName { get; set; }
        public string Gender { get; set; }
        public string Race { get; set; }
        public string Class { get; set; }
        public string CurrentLocation { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Mana { get; set; }
        public int MaxMana { get; set; }
        public int Stamina { get; set; }
        public int MaxStamina { get; set; }
        public int Defense { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastPlayed { get; set; }
        public bool IsActive { get; set; }
        public int PlayTimeMinutes { get; set; }

        public PlayerSave(long chatId)
        {
            ChatId = chatId;
            CreatedAt = DateTime.Now;
            LastPlayed = DateTime.Now;
            IsActive = true;
            PlayTimeMinutes = 0;
        }
    }
}