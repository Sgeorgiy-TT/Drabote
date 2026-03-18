using System;
using System.ComponentModel.DataAnnotations;

namespace TelegramMetroidvaniaBot.Models
{
    public class PlayerSave
    {
        [Key]
        public long ChatId { get; set; }
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
        public double ExperienceMultiplier { get; set; }
        public double MeleeDamageMultiplier { get; set; }
        public double RangedDamageMultiplier { get; set; }
        public double MagicDamageMultiplier { get; set; }
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