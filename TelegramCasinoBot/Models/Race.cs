namespace TelegramMetroidvaniaBot.Models
{
    public class Race
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int HealthBonus { get; set; }
        public int ManaBonus { get; set; }
        public int StaminaBonus { get; set; }
        public int DefenseBonus { get; set; }
        public double ExperienceMultiplier { get; set; }
        public double MeleeDamageBonus { get; set; }
        public double RangedDamageBonus { get; set; }
        public double MagicDamageBonus { get; set; }
        public string[] AvailableGenders { get; set; }
        public string[] SpecialAbilities { get; set; }
    }
}