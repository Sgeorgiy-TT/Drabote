using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Data;
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

        private readonly Dictionary<long, BattleState> _battles = new();

        public BattleService(
            TelegramBotClient botClient,
            GameWorld world,
            LocationService locationService,
            PlayerService playerService,
            ILogger<BattleService> logger,
            MobService mobService,
            ItemService itemService)
        {
            _logger = logger;
            _botClient = botClient;
            _world = world;
            _locationService = locationService;
            _playerService = playerService;
            _mobService = mobService;
            _itemService = itemService;
        }

        public async Task StartMobBattle(long chatId, Player player, MobInstance mobInstance)
        {
            if (_battles.ContainsKey(chatId))
                return;

            var mobData = _mobService.GetMobById(mobInstance.MobId);
            if (mobData == null) return;

            var state = new BattleState
            {
                ChatId = chatId,
                Player = player,
                CurrentMob = mobInstance,
                MobData = mobData,
                IsBossBattle = false,
                MessageId = 0,
                Stage = BattleStage.ActionSelection,
                PlayerDefending = false
            };
            _battles[chatId] = state;

            var keyboard = GetMobBattleKeyboard();
            var text = FormatBattleStatus(state);
            var msg = await _botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
            state.MessageId = msg.MessageId;
        }

        public async Task HandleMobAction(long chatId, string action, CallbackQuery callbackQuery)
        {
            if (!_battles.TryGetValue(chatId, out var state) || state.IsBossBattle)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Бой не найден");
                return;
            }

            var rng = new Random();
            string resultMessage = "";

            switch (action)
            {
                case "mob_attack":
                    int playerDamage = rng.Next(10, 25);
                    state.CurrentMob.CurrentHealth -= playerDamage;
                    resultMessage += $"💥 Вы нанесли {playerDamage} урона!\n";
                    break;

                case "mob_defend":
                    resultMessage += $"🛡️ Вы защитились! Урон будет снижен вдвое.\n";
                    state.PlayerDefending = true;
                    break;

                case "mob_ability":
                    if (state.Player.Mana.Current < 15)
                    {
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Недостаточно маны");
                        return;
                    }
                    state.Player.Mana.Current -= 15;
                    int abilityDamage = rng.Next(25, 40);
                    state.CurrentMob.CurrentHealth -= abilityDamage;
                    resultMessage += $"✨ Вы применили способность! Нанесено {abilityDamage} урона.\n";
                    break;

                case "mob_item":
                    var healItemName = state.Player.Inventory.FirstOrDefault(i =>
                        _itemService.GetItemByName(i)?.ItemType == "consumable" &&
                        _itemService.GetItemByName(i)?.EffectType == "health");
                    if (healItemName == null)
                    {
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Нет лечебных зелий");
                        return;
                    }
                    var healItem = await _itemService.GetItemByNameAsync(healItemName);
                    state.Player.Inventory.Remove(healItemName);
                    int healAmount = healItem.Value ?? 30;
                    state.Player.Health.Current += healAmount;
                    resultMessage += $"💊 Вы использовали {healItem.Name} и восстановили {healAmount} HP.\n";
                    break;

                case "mob_flee":
                    int fleeChance = rng.Next(1, 101);
                    if (fleeChance <= 50)
                    {
                        resultMessage = "🏃‍♂️ Вы успешно сбежали! Бой окончен.";
                        await EndBattle(chatId, state, false);
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        await _botClient.EditMessageTextAsync(chatId, state.MessageId, resultMessage);
                        return;
                    }
                    else
                    {
                        resultMessage = "🏃‍♂️ Попытка побега не удалась!\n";
                    }
                    break;
            }

            if (state.CurrentMob.CurrentHealth <= 0)
            {
                await EndBattle(chatId, state, true);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                await _botClient.EditMessageTextAsync(chatId, state.MessageId, "🎉 Вы победили моба!");
                return;
            }

            int mobDamage = 0;
            if (action != "mob_flee")
            {
                mobDamage = rng.Next(state.MobData.DamageMin, state.MobData.DamageMax + 1);
                if (state.PlayerDefending)
                {
                    mobDamage /= 2;
                    state.PlayerDefending = false;
                }
                state.Player.Health.Current -= mobDamage;
                resultMessage += $"⚔️ {state.MobData.Name} атаковал и нанёс {mobDamage} урона.\n";
            }

            if (state.Player.Health.Current <= 0)
            {
                await EndBattle(chatId, state, false);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                await _botClient.EditMessageTextAsync(chatId, state.MessageId, "💀 Вы погибли в бою...");
                return;
            }

            var status = FormatBattleStatus(state);
            var fullText = $"{status}\n\n{resultMessage}";
            await _botClient.EditMessageTextAsync(chatId, state.MessageId, fullText, parseMode: ParseMode.Markdown, replyMarkup: GetMobBattleKeyboard());
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private async Task EndBattle(long chatId, BattleState state, bool victory)
        {
            _battles.Remove(chatId);
            if (victory && !state.IsBossBattle)
            {
                var locMobs = state.Player.LocationMobs[state.Player.CurrentLocation];
                locMobs.Remove(state.CurrentMob);
                await _playerService.AddExperience(chatId, state.Player, state.MobData.ExperienceReward);
                await _botClient.SendTextMessageAsync(chatId, $"⭐ +{state.MobData.ExperienceReward} опыта!");
            }
            else if (victory && state.IsBossBattle)
            {
                // босс побеждён
                state.Player.BossHealth = 0;
                state.Player.QuestCompleted.Add("defeat_guardian");
                await _playerService.AddExperience(chatId, state.Player, 150);
                if (!state.Player.Abilities.Contains("Сила Древних"))
                {
                    state.Player.Abilities.Add("Сила Древних");
                    await _botClient.SendTextMessageAsync(chatId, "💪 *Получена новая способность: Сила Древних!*", parseMode: ParseMode.Markdown);
                }
                state.Player.CurrentLocation = "final_sanctum";
                await _locationService.DescribeLocation(chatId, state.Player);
            }
            else
            {
                state.Player.Health.Current = state.Player.Health.Max / 2;
                if (state.IsBossBattle)
                    state.Player.CurrentLocation = "crystal_cave";
                await _locationService.DescribeLocation(chatId, state.Player);
            }
        }

        private string FormatBattleStatus(BattleState state)
        {
            if (!state.IsBossBattle)
            {
                return $@"⚔️ *БИТВА С {state.MobData.Name.ToUpper()}* (Ур. {state.MobData.Level})

❤️ Ваше здоровье: {state.Player.Health.Current}/{state.Player.Health.Max}
🔮 Мана: {state.Player.Mana.Current}/{state.Player.Mana.Max}
👹 Здоровье моба: {state.CurrentMob.CurrentHealth}/{state.MobData.Health}";
            }
            else
            {
                return $@"⚔️ *БИТВА СО СТРАЖЕМ ВРАТ*

❤️ Ваше здоровье: {state.Player.Health.Current}/{state.Player.Health.Max}
🔮 Мана: {state.Player.Mana.Current}/{state.Player.Mana.Max}
👹 Здоровье стража: {state.BossHealth}/{state.BossMaxHealth}";
            }
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

        // ======================== БОСС ========================

        public async Task StartBossBattle(long chatId, Player player)
        {
            if (_battles.ContainsKey(chatId))
                return;

            int bossMaxHealth = 150;
            int currentBossHealth = player.BossHealth > 0 ? player.BossHealth : bossMaxHealth;

            var state = new BattleState
            {
                ChatId = chatId,
                Player = player,
                IsBossBattle = true,
                BossHealth = currentBossHealth,
                BossMaxHealth = bossMaxHealth,
                MessageId = 0,
                Stage = BattleStage.ActionSelection,
                PlayerDefending = false
            };
            _battles[chatId] = state;

            var keyboard = GetBossBattleKeyboard();
            var text = FormatBattleStatus(state);
            var msg = await _botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
            state.MessageId = msg.MessageId;
        }

        public async Task HandleBossAction(long chatId, string action, CallbackQuery callbackQuery)
        {
            if (!_battles.TryGetValue(chatId, out var state) || !state.IsBossBattle)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Бой не найден");
                return;
            }

            var rng = new Random();
            string resultMessage = "";

            switch (action)
            {
                case "boss_attack":
                    int playerDamage = rng.Next(15, 30);
                    state.BossHealth -= playerDamage;
                    resultMessage += $"💥 Вы нанесли {playerDamage} урона!\n";
                    break;

                case "boss_defend":
                    resultMessage += $"🛡️ Вы защитились! Урон будет снижен вдвое.\n";
                    state.PlayerDefending = true;
                    break;

                case "boss_ability":
                    if (state.Player.Mana.Current < 20)
                    {
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Недостаточно маны");
                        return;
                    }
                    state.Player.Mana.Current -= 20;
                    int abilityDamage = rng.Next(25, 40);
                    state.BossHealth -= abilityDamage;
                    resultMessage += $"✨ Вы использовали Лазерный луч! Нанесено {abilityDamage} урона.\n";
                    break;

                case "boss_flee":
                    int fleeChance = rng.Next(1, 101);
                    if (fleeChance <= 50)
                    {
                        resultMessage = "🏃‍♂️ Вы успешно сбежали! Бой окончен.";
                        await EndBattle(chatId, state, false);
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                        await _botClient.EditMessageTextAsync(chatId, state.MessageId, resultMessage);
                        return;
                    }
                    else
                    {
                        resultMessage = "🏃‍♂️ Попытка побега не удалась!\n";
                    }
                    break;
            }

            if (state.BossHealth <= 0)
            {
                await EndBattle(chatId, state, true);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                await _botClient.EditMessageTextAsync(chatId, state.MessageId, "🎉 Вы победили Стража!");
                return;
            }

            int bossDamage = rng.Next(10, 20);
            if (state.PlayerDefending)
            {
                bossDamage /= 2;
                state.PlayerDefending = false;
            }
            state.Player.Health.Current -= bossDamage;
            resultMessage += $"⚔️ Страж атаковал и нанёс {bossDamage} урона.\n";

            if (state.Player.Health.Current <= 0)
            {
                await EndBattle(chatId, state, false);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                await _botClient.EditMessageTextAsync(chatId, state.MessageId, "💀 Вы погибли в бою...");
                return;
            }

            var status = FormatBattleStatus(state);
            var fullText = $"{status}\n\n{resultMessage}";
            await _botClient.EditMessageTextAsync(chatId, state.MessageId, fullText, parseMode: ParseMode.Markdown, replyMarkup: GetBossBattleKeyboard());
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private InlineKeyboardMarkup GetBossBattleKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⚔️ Атака", "boss_attack"), InlineKeyboardButton.WithCallbackData("🛡️ Защита", "boss_defend") },
                new[] { InlineKeyboardButton.WithCallbackData("✨ Способность", "boss_ability"), InlineKeyboardButton.WithCallbackData("🏃‍♂️ Бегство", "boss_flee") }
            });
        }
    }
}