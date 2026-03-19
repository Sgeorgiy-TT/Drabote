namespace TelegramMetroidvaniaBot.Models
{
    public abstract class CharacterStats
    {
        public int HealthBonus { get; set; }
        public int ManaBonus { get; set; }
        public int StaminaBonus { get; set; }
        public int DefenseBonus { get; set; }

        public double ExperienceMultiplier { get; set; } = 1.0;
        public double MeleeDamageMultiplier { get; set; } = 1.0;
        public double RangedDamageMultiplier { get; set; } = 1.0;
        public double MagicDamageMultiplier { get; set; } = 1.0;

        protected CharacterStats() { }
    }
}