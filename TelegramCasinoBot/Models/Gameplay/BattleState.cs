using System.Collections.Generic;

namespace TelegramCasinoBot.Models.Gameplay
{
    public class BattleState
    {
        public long ChatId { get; set; }
        public Player Player { get; set; }
        public MobInstance CurrentMob { get; set; }
        public Mob MobData { get; set; }
        public bool IsBossBattle { get; set; } 
        public int MessageId { get; set; }
        public BattleStage Stage { get; set; } = BattleStage.ActionSelection;
        public List<Ability> AvailableAbilities { get; set; }
        public List<Item> AvailableItems { get; set; }

        public int BossHealth { get; set; }
        public int BossMaxHealth { get; set; }

        public bool InBattle { get; set; } = true;
        public bool PlayerDefending { get; set; }
    }

    public enum BattleStage
    {
        ActionSelection,
        SelectingAbility,
        SelectingItem,
        Waiting
    }
}