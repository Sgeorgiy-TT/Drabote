using System.Text.Json.Serialization;

namespace TelegramCasinoBot.Models.Gameplay
{
    public class MobInstance
    {
        public int MobId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int CurrentHealth { get; set; }
        public int CurrentMana { get; set; }
        public int CurrentStamina { get; set; }

        public MobInstance() { }

        [JsonConstructor]
        public MobInstance(int mobId, int x, int y, int currentHealth, int currentMana, int currentStamina)
        {
            MobId = mobId;
            X = x;
            Y = y;
            CurrentHealth = currentHealth;
            CurrentMana = currentMana;
            CurrentStamina = currentStamina;
        }
    }
}