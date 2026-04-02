namespace TelegramCasinoBot.Models.Character
{
    public abstract class CharacterStats
    {
        public int HealthBonus { get; }
        public int ManaBonus { get; }
        public int StaminaBonus { get; }
        public int DefenseBonus { get; }
        public double ExperienceMultiplier { get; }
        public double MeleeDamageMultiplier { get; }
        public double RangedDamageMultiplier { get; }
        public double MagicDamageMultiplier { get; }

        protected CharacterStats(
            int healthBonus, int manaBonus, int staminaBonus, int defenseBonus,
            double experienceMultiplier, double meleeDamageMultiplier,
            double rangedDamageMultiplier, double magicDamageMultiplier)
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