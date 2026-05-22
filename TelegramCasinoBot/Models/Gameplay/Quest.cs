using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramCasinoBot.Models.Gameplay
{
    public class Quest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public int TargetMobId { get; set; }
        public int RequiredCount { get; set; }
        public int RewardExp { get; set; }
        public int RewardGold { get; set; }
        public int? RewardItemId { get; set; }
        public int? NextQuestId { get; set; }
    }
}