using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Data.Gameplay;
using TelegramCasinoBot.Services.Models.DataStats;
using TelegramCasinoBot.Services.Models.Gameplay.Location;

namespace TelegramCasinoBot.Services.Models.Gameplay
{
    public class BattleService
    {
        private readonly ILogger<BattleService> _logger;
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly LocationService _locationService;
        private readonly PlayerService _playerService;
        private readonly MobService _mobService;
        private readonly ItemService _itemService;
        private readonly AbilityService _abilityService;
        private readonly QuestService _questService;
        private readonly DatabaseService _databaseService;

        private readonly Dictionary<long, BattleState> _battles = new();

        public BattleService(
            TelegramBotClient botClient,
            GameWorld world,
            LocationService locationService,
            PlayerService playerService,
            ILogger<BattleService> logger,
            MobService mobService,
            ItemService itemService,
            AbilityService abilityService,
            QuestService questService,
            DatabaseService databaseService)
        {
            _logger = logger;
            _botClient = botClient;
            _world = world;
            _locationService = locationService;
            _playerService = playerService;
            _mobService = mobService;
            _itemService = itemService;
            _abilityService = abilityService;
            _questService = questService;
            _databaseService = databaseService;
        }

        public async Task StartMobBattle(long chatId, Player player, MobInstance mobInstance)
        {
            if (_battles.ContainsKey(chatId))
                return;

            var mobData = _mobService.GetMobById(mobInstance.MobId);
            if (mobData == null) return;

            if (!string.IsNullOrEmpty(mobData.ImagePath) && System.IO.File.Exists(mobData.ImagePath))
            {
                using var stream = System.IO.File.OpenRead(mobData.ImagePath);
                await _botClient.SendPhotoAsync(chatId, new InputOnlineFile(stream, "mob.jpg"),
                    caption: $"⚔️ *Вы встретили {mobData.Name}!*", parseMode: ParseMode.Markdown);
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, $"⚔️ *Вы встретили {mobData.Name}!*", parseMode: ParseMode.Markdown);
            }

            var state = new BattleState
            {
                ChatId = chatId,
                Player = player,
                CurrentMob = mobInstance,
                MobData = mobData,
                IsBossBattle = false,
                MessageId = 0,
                Stage = BattleStage.ActionSelection,
                InBattle = true,
                PlayerEffects = new List<ActiveEffect>(),
                MobEffects = new List<ActiveEffect>()
            };
            _battles[chatId] = state;

            var keyboard = GetMobBattleKeyboard();
            var text = FormatBattleStatus(state);
            var msg = await _botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
            state.MessageId = msg.MessageId;
        }

        

        public async Task HandleMobAction(long chatId, string action, CallbackQuery callbackQuery)
        {
            if (!_battles.TryGetValue(chatId, out var state))
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Бой не найден");
                return;
            }

            if (state.Stage == BattleStage.SelectingAbility && action != "back_to_battle")
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Сначала выберите способность или вернитесь назад");
                return;
            }

            var rng = new Random();
            string resultMessage = "";

            switch (action)
            {
                case "mob_attack":
                    int baseDamage = rng.Next(10, 25);
                    int playerDamage = baseDamage + state.Player.TotalStrength;
                    playerDamage = ApplyDamageModifiers(state, playerDamage, true);
                    if (state.IsBossBattle)
                        state.BossHealth -= playerDamage;
                    else
                        state.CurrentMob.CurrentHealth -= playerDamage;
                    resultMessage += $"💥 Вы нанесли {playerDamage} урона!\n";
                    break;

                case "mob_defend":
                    resultMessage += $"🛡️ Вы защитились! Урон будет снижен вдвое.\n";
                    state.PlayerDefending = true;
                    break;

                case "mob_ability":
                    state.Stage = BattleStage.SelectingAbility;
                    await ShowAbilitySelection(chatId, state);
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;

                case "mob_item":
                    state.Stage = BattleStage.SelectingItem;
                    await ShowItemSelection(chatId, state);
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;

                case "mob_flee":
                    int fleeChance = rng.Next(1, 101);
                    if (fleeChance <= 50)
                    {
                        await EndBattle(chatId, state, false);
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        await _botClient.EditMessageTextAsync(chatId, state.MessageId, "🏃‍♂️ Вы успешно сбежали! Бой окончен.");
                        return;
                    }
                    else
                    {
                        resultMessage = "🏃‍♂️ Попытка побега не удалась!\n";
                    }
                    break;

                case "back_to_battle":
                    state.Stage = BattleStage.ActionSelection;
                    await ReturnToBattle(chatId, state);
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                    return;
            }

            bool mobDefeated = state.IsBossBattle ? state.BossHealth <= 0 : state.CurrentMob.CurrentHealth <= 0;
            if (mobDefeated)
            {
                await EndBattle(chatId, state, true);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                await _botClient.EditMessageTextAsync(chatId, state.MessageId, "🎉 Вы победили!");
                return;
            }

            await PerformMobTurn(chatId, state, resultMessage);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        public async Task HandleAbilitySelection(long chatId, int abilityId, CallbackQuery callbackQuery)
        {
            if (!_battles.TryGetValue(chatId, out var state)) return;
            if (state.Stage != BattleStage.SelectingAbility) return;

            var ability = state.Player.LearnedAbilities.FirstOrDefault(a => a.Id == abilityId);
            if (ability == null) return;

            if (ability.ManaCost > 0 && state.Player.Mana.Current < ability.ManaCost)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Недостаточно маны");
                return;
            }
            if (ability.StaminaCost > 0 && state.Player.Stamina.Current < ability.StaminaCost)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Недостаточно выносливости");
                return;
            }

            state.Player.Mana.Current -= ability.ManaCost;
            state.Player.Stamina.Current -= ability.StaminaCost;

            string resultMessage = $"✨ Вы использовали {ability.Name}!";

            if (ability.Type == "attack")
            {
                int totalDamage = ability.Damage + state.Player.TotalStrength;
                if (ability.Target == "enemy")
                {
                    if (state.IsBossBattle)
                        state.BossHealth -= totalDamage;
                    else
                        state.CurrentMob.CurrentHealth -= totalDamage;
                    resultMessage += $"\n💥 Нанесено {totalDamage} урона!";
                }
                else if (ability.Target == "self" && ability.Type == "heal")
                {
                    int heal = -ability.Damage;
                    state.Player.Health.Current += heal;
                    resultMessage += $"\n❤️ Вы восстановили {heal} здоровья!";
                }
            }
            else if (ability.Type == "heal")
            {
                int heal = -ability.Damage;
                state.Player.Health.Current += heal;
                resultMessage += $"\n❤️ Вы восстановили {heal} здоровья!";
            }

            if (ability.Effects != null)
            {
                foreach (var effect in ability.Effects)
                {
                    var targetEffects = ability.Target == "enemy" ? state.MobEffects : state.PlayerEffects;
                    targetEffects.Add(new ActiveEffect(effect.Type, effect.Value, effect.Duration, effect.Stackable));
                    resultMessage += $"\n✨ Наложен эффект: {effect.Type} на {effect.Duration} ходов.";
                }
            }

            state.Stage = BattleStage.Waiting;
            await _botClient.EditMessageTextAsync(chatId, state.MessageId, resultMessage, parseMode: ParseMode.Markdown);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);

            bool mobDefeated = state.IsBossBattle ? state.BossHealth <= 0 : state.CurrentMob.CurrentHealth <= 0;
            if (mobDefeated)
            {
                await EndBattle(chatId, state, true);
                await _botClient.EditMessageTextAsync(chatId, state.MessageId, "🎉 Вы победили!");
                return;
            }

            await PerformMobTurn(chatId, state, "");
        }

        private async Task ShowAbilitySelection(long chatId, BattleState state)
        {
            var abilities = state.Player.LearnedAbilities
                .Where(a => a.MinLevel <= state.Player.Level)
                .ToList();

            if (abilities.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ У вас нет доступных способностей.", replyMarkup: GetMobBattleKeyboard());
                state.Stage = BattleStage.ActionSelection;
                return;
            }

            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var ability in abilities)
            {
                string costText = "";
                if (ability.ManaCost > 0) costText += $" {ability.ManaCost}🔮";
                if (ability.StaminaCost > 0) costText += $" {ability.StaminaCost}💪";
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{ability.Name}{costText}", $"ability_select_{ability.Id}")
                });
            }
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "back_to_battle") });

            var keyboard = new InlineKeyboardMarkup(buttons);
            await _botClient.EditMessageTextAsync(chatId, state.MessageId, "🎯 *Выберите способность:*", parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        private async Task ShowItemSelection(long chatId, BattleState state)
        {
            var consumables = state.Player.Inventory
                .Select(name => _itemService.GetItemByName(name))
                .Where(i => i != null && i.ItemType == "consumable")
                .ToList();

            if (consumables.Count == 0)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Нет доступных предметов.", replyMarkup: GetMobBattleKeyboard());
                state.Stage = BattleStage.ActionSelection;
                return;
            }

            var buttons = new List<InlineKeyboardButton[]>();
            foreach (var item in consumables)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData($"💊 {item.Name}", $"item_use_{item.Id}")
                });
            }
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "back_to_battle") });

            var keyboard = new InlineKeyboardMarkup(buttons);
            await _botClient.EditMessageTextAsync(chatId, state.MessageId, "🎒 *Выберите предмет:*", parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        private async Task ReturnToBattle(long chatId, BattleState state)
        {
            var status = FormatBattleStatus(state);
            var keyboard = GetMobBattleKeyboard();
            await _botClient.EditMessageTextAsync(chatId, state.MessageId, status, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        private async Task PerformMobTurn(long chatId, BattleState state, string resultMessagePrefix)
        {
            var rng = new Random();
            Ability ability = GetRandomMobAbility(state.MobData);

            int damage = 0;
            string abilityName = ability?.Name ?? (state.IsBossBattle ? "атака голема" : "атака моба");
            string resultMessage = resultMessagePrefix;

            if (ability != null && ability.Type == "attack")
            {
                damage = ability.Damage;
                int ignoreArmor = ability.IgnoreArmor;
                int finalDamage = Math.Max(1, damage - Math.Max(0, state.Player.TotalDefense - ignoreArmor));
                if (state.PlayerDefending)
                {
                    finalDamage /= 2;
                    state.PlayerDefending = false;
                }
                state.Player.Health.Current -= finalDamage;
                resultMessage += $"⚔️ {state.MobData.Name} использовал *{abilityName}* и нанёс {finalDamage} урона.\n";

                if (ability.Effects != null)
                {
                    foreach (var effect in ability.Effects)
                    {
                        state.PlayerEffects.Add(new ActiveEffect(effect.Type, effect.Value, effect.Duration, effect.Stackable));
                        resultMessage += $"\n✨ Наложен эффект: {effect.Type} на {effect.Duration} ходов.";
                    }
                }
            }
            else if (ability != null && ability.Type == "heal")
            {
                int heal = -ability.Damage;
                if (state.IsBossBattle)
                    state.BossHealth = Math.Min(state.BossMaxHealth, state.BossHealth + heal);
                else
                    state.CurrentMob.CurrentHealth = Math.Min(state.MobData.Health, state.CurrentMob.CurrentHealth + heal);
                resultMessage += $"💚 {state.MobData.Name} использовал *{abilityName}* и восстановил {heal} здоровья.\n";
            }
            else if (ability != null && ability.Type == "buff")
            {
                foreach (var effect in ability.Effects)
                {
                    state.MobEffects.Add(new ActiveEffect(effect.Type, effect.Value, effect.Duration, effect.Stackable));
                    resultMessage += $"\n✨ {state.MobData.Name} усилил себя: {effect.Type} на {effect.Duration} ходов.";
                }
                resultMessage += $"\n{state.MobData.Name} использовал *{abilityName}*.\n";
            }
            else
            {
                int defaultDamage = state.IsBossBattle
                    ? rng.Next(state.MobData.DamageMin, state.MobData.DamageMax + 1)
                    : rng.Next(state.MobData.DamageMin, state.MobData.DamageMax + 1);
                int finalDamage = Math.Max(1, defaultDamage - state.Player.TotalDefense);
                if (state.PlayerDefending)
                {
                    finalDamage /= 2;
                    state.PlayerDefending = false;
                }
                state.Player.Health.Current -= finalDamage;
                resultMessage += $"⚔️ {state.MobData.Name} атаковал и нанёс {finalDamage} урона.\n";
            }

            if (state.Player.Health.Current <= 0)
            {
                await EndBattle(chatId, state, false);
                await _botClient.EditMessageTextAsync(chatId, state.MessageId, "💀 Вы погибли в бою...");
                return;
            }

            var status = FormatBattleStatus(state);
            var fullText = $"{status}\n\n{resultMessage}";
            var keyboard = state.IsBossBattle ? GetBossBattleKeyboard() : GetMobBattleKeyboard();
            await _botClient.EditMessageTextAsync(chatId, state.MessageId, fullText, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
            state.Stage = BattleStage.ActionSelection;
        }

        private int ApplyDamageModifiers(BattleState state, int baseDamage, bool isPlayer)
        {
            var effects = isPlayer ? state.PlayerEffects : state.MobEffects;
            int damage = baseDamage;

            foreach (var effect in effects)
            {
                if (effect.Type == "attack_buff" && isPlayer)
                    damage += effect.Value;
                else if (effect.Type == "attack_debuff" && !isPlayer)
                    damage -= effect.Value;
                else if (effect.Type == "defense_buff" && !isPlayer)
                    damage -= effect.Value;
                else if (effect.Type == "defense_debuff" && isPlayer)
                    damage += effect.Value;
                else if (effect.Type == "burn" && !isPlayer)
                    damage += effect.Value;
                else if (effect.Type == "poison" && !isPlayer)
                    damage += effect.Value;
            }

            damage = Math.Max(1, damage);
            foreach (var effect in effects)
            {
                effect.DecrementDuration();
            }
            effects.RemoveAll(e => e.IsExpired);

            return damage;
        }

        private async Task EndBattle(long chatId, BattleState state, bool victory)
        {
            _battles.Remove(chatId);
            if (victory)
            {
                if (!state.IsBossBattle)
                {
                    var locMobs = state.Player.LocationMobs[state.Player.CurrentLocation];
                    locMobs.Remove(state.CurrentMob);

                    await _playerService.AddExperience(chatId, state.Player, state.MobData.ExperienceReward);

                    if (state.MobData.GoldReward > 0)
                    {
                        state.Player.Gold += state.MobData.GoldReward;
                        await _botClient.SendTextMessageAsync(chatId, $"💰 +{state.MobData.GoldReward} золота!");
                    }

                    var drop = await _mobService.GetRandomDropAsync(state.MobData);
                    if (drop != null)
                    {
                        state.Player.Inventory.Add(drop.Name);
                        await _botClient.SendTextMessageAsync(chatId, $"🎁 Вы получили: {drop.GetDisplayName()}");
                    }

                    await _botClient.SendTextMessageAsync(chatId, $"⭐ +{state.MobData.ExperienceReward} опыта!");

                    var quest = _questService.GetQuestById(state.Player.CurrentQuestId);
                    if (quest != null && quest.Type == "kill" && quest.TargetMobId == state.MobData.Id)
                    {
                        var progress = state.Player.QuestProgress.FirstOrDefault(p => p.QuestId == quest.Id);
                        if (progress == null)
                        {
                            progress = new QuestProgress { QuestId = quest.Id, CurrentCount = 0, IsCompleted = false };
                            state.Player.QuestProgress.Add(progress);
                        }
                        if (!progress.IsCompleted)
                        {
                            progress.CurrentCount++;
                            if (progress.CurrentCount >= quest.RequiredCount)
                            {
                                progress.IsCompleted = true;
                                await _playerService.AddExperience(chatId, state.Player, quest.RewardExp);
                                state.Player.Gold += quest.RewardGold;
                                if (quest.RewardItemId.HasValue)
                                {
                                    var rewardItem = _itemService.GetItemById(quest.RewardItemId.Value);
                                    if (rewardItem != null)
                                        state.Player.Inventory.Add(rewardItem.Name);
                                }
                                if (quest.NextQuestId.HasValue)
                                    state.Player.CurrentQuestId = quest.NextQuestId.Value;
                                await _botClient.SendTextMessageAsync(chatId, $"🎉 Квест выполнен! +{quest.RewardExp} опыта, +{quest.RewardGold} золота");
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync(chatId, $"📜 Прогресс квеста: {progress.CurrentCount}/{quest.RequiredCount}");
                            }
                        }
                    }
                    if (state.MobData.Id == 5)
                    {
                        state.Player.BossHealth = 0;
                        state.Player.QuestCompleted.Add("defeat_guardian");

                        await _botClient.SendTextMessageAsync(chatId,
                            "🎉 *ПОЗДРАВЛЯЕМ!* 🎉\n\n" +
                            "Вы победили могучего Голема – стража глубин!\n" +
                            "Ваше имя вписано в летопись героев.\n" +
                            "Теперь вам открыт путь в Глубины, где ждут новые приключения.\n\n" +
                            "✨ *Славная победа!* ✨",
                            parseMode: ParseMode.Markdown);

                        await _locationService.DescribeLocation(chatId, state.Player);
                    }
                    await _databaseService.SavePlayerAsync(state.Player);
                }
            }
            else
            {
                state.Player.Health.Current = state.Player.Health.Max / 2;
                if (!state.IsBossBattle)
                    await _locationService.DescribeLocation(chatId, state.Player);
                else
                {
                    state.Player.CurrentLocation = "crystal_cave";
                    await _locationService.DescribeLocation(chatId, state.Player);
                }
                await _databaseService.SavePlayerAsync(state.Player);
            }
        }

        private string FormatBattleStatus(BattleState state)
        {
            var result = $"⚔️ *БИТВА С {state.MobData.Name.ToUpper()}* (Ур. {state.MobData.Level})\n\n";
            result += $"*{state.Player.Name}*\n";
            result += $"❤️ Здоровье: {state.Player.Health.Current}/{state.Player.Health.Max}\n";
            result += $"🔮 Мана: {state.Player.Mana.Current}/{state.Player.Mana.Max}\n";
            result += $"💪 Выносливость: {state.Player.Stamina.Current}/{state.Player.Stamina.Max}\n\n";
            result += $"*{state.MobData.Name}*\n";
            result += $"👹 Здоровье: {state.CurrentMob.CurrentHealth}/{state.MobData.Health}\n";
            result += $"---\n";
            return result;
        }
        

        private InlineKeyboardMarkup GetMobBattleKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⚔️ Атака", "mob_attack"), InlineKeyboardButton.WithCallbackData("🛡️ Защита", "mob_defend") },
                new[] { InlineKeyboardButton.WithCallbackData("✨ Способность", "mob_ability"), InlineKeyboardButton.WithCallbackData("🏃‍♂️ Бегство", "mob_flee") },
                new[] { InlineKeyboardButton.WithCallbackData("🎒 Инвентарь", "mob_item") }
            });
        }

        private InlineKeyboardMarkup GetBossBattleKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⚔️ Атака", "mob_attack"), InlineKeyboardButton.WithCallbackData("🛡️ Защита", "mob_defend") },
                new[] { InlineKeyboardButton.WithCallbackData("✨ Способность", "mob_ability"), InlineKeyboardButton.WithCallbackData("🏃‍♂️ Бегство", "mob_flee") },
                new[] { InlineKeyboardButton.WithCallbackData("🎒 Инвентарь", "mob_item") }
            });
        }
        public async Task HandleItemUse(long chatId, int itemId, CallbackQuery callbackQuery)
        {
            if (!_battles.TryGetValue(chatId, out var state))
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Бой не найден");
                return;
            }
            if (state.Stage != BattleStage.SelectingItem)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Не время для использования предметов");
                return;
            }

            var item = _itemService.GetItemById(itemId);
            if (item == null || item.ItemType != "consumable")
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Нельзя использовать этот предмет");
                return;
            }

            if (!state.Player.Inventory.Contains(item.Name))
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ У вас нет этого предмета");
                return;
            }

            string resultMessage = "";
            if (item.EffectType == "health")
            {
                int heal = item.Value ?? 30;
                state.Player.Health.Current += heal;
                resultMessage = $"💊 Вы использовали {item.Name} и восстановили {heal} HP.";
            }
            else if (item.EffectType == "mana")
            {
                int mana = item.Value ?? 20;
                state.Player.Mana.Current += mana;
                resultMessage = $"💙 Вы использовали {item.Name} и восстановили {mana} MP.";
            }
            else if (item.EffectType == "stamina")
            {
                int stamina = item.Value ?? 20;
                state.Player.Stamina.Current += stamina;
                resultMessage = $"💪 Вы использовали {item.Name} и восстановили {stamina} выносливости.";
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Эффект предмета не поддерживается");
                return;
            }

            state.Player.Inventory.Remove(item.Name);
            state.Stage = BattleStage.ActionSelection;
            await _databaseService.SavePlayerAsync(state.Player);

            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, resultMessage);
            await ReturnToBattle(chatId, state);
        }

        public async Task HandleBackToBattle(long chatId, CallbackQuery callbackQuery)
        {
            if (!_battles.TryGetValue(chatId, out var state))
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Бой не найден");
                return;
            }
            state.Stage = BattleStage.ActionSelection;
            await ReturnToBattle(chatId, state);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }
        private Ability GetRandomMobAbility(Mob mobData)
        {
            var abilities = _abilityService.GetMobAbilities()
                .Where(a => a.MobId == mobData.Id && a.MinLevel <= mobData.Level)
                .ToList();
            if (!abilities.Any()) return null;
            var rng = new Random();
            var eligible = abilities.Where(a => rng.NextDouble() <= a.Probability).ToList();
            if (!eligible.Any()) eligible = abilities;
            return eligible[rng.Next(eligible.Count)];
        }
    }
}