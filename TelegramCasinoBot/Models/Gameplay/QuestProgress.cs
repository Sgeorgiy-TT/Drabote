using System.Collections.Generic;

namespace TelegramCasinoBot.Models.Gameplay
{
    public class QuestProgress
    {
        public int QuestId { get; set; }
        public int CurrentCount { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsRewardClaimed { get; set; }
    }
}