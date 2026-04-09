namespace TelegramCasinoBot.Models.Character
{
    public abstract class CharacterStats
    {
        public int HealthBonus { get; set; }
        public int ManaBonus { get; set; }
        public int StaminaBonus { get; set; }
        public int DefenseBonus { get; set; }
        public double ExperienceMultiplier { get; set; }
        public double MeleeDamageMultiplier { get; set; }
        public double RangedDamageMultiplier { get; set; }
        public double MagicDamageMultiplier { get; set; }

        public CharacterStats()
        {
            HealthBonus = 0;
            ManaBonus = 0;
            StaminaBonus = 0;
            DefenseBonus = 0;
            ExperienceMultiplier = 1.0;
            MeleeDamageMultiplier = 1.0;
            RangedDamageMultiplier = 1.0;
            MagicDamageMultiplier = 1.0;
        }

        public CharacterStats(
            int healthBonus,
            int manaBonus,
            int staminaBonus,
            int defenseBonus,
            double experienceMultiplier,
            double meleeDamageMultiplier,
            double rangedDamageMultiplier,
            double magicDamageMultiplier)
        {
            HealthBonus = healthBonus;
            ManaBonus = manaBonus;
            StaminaBonus = staminaBonus;
            DefenseBonus = defenseBonus;
            ExperienceMultiplier = experienceMultiplier;
            MeleeDamageMultiplier = meleeDamageMultiplier;
            RangedDamageMultiplier = rangedDamageMultiplier;
            MagicDamageMultiplier = magicDamageMultiplier;
        }
    }
}