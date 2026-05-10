using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public MobInstance(int mobId, int x, int y, int maxHealth, int maxMana, int maxStamina)
        {
            MobId = mobId;
            X = x;
            Y = y;
            CurrentHealth = maxHealth;
            CurrentMana = maxMana;
            CurrentStamina = maxStamina;
        }
    }
}
