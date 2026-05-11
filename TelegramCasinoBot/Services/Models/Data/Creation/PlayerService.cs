using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Models.Data.Creation;
using TelegramCasinoBot.Services.Models.Data.Gameplay;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.Models.DataStats
{
    public class PlayerService
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly ILogger<PlayerService> _logger;

        public PlayerService(TelegramBotClient botClient, GameWorld world, ILogger<PlayerService> logger = null)
        {
            _botClient = botClient;
            _world = world;
            _logger = logger ?? NullLogger<PlayerService>.Instance;
        }

        public async Task AddExperience(long chatId, Player player, int exp)
        {
            _logger.LogDebug("Начало AddExperience для chatId {ChatId}, exp {Exp}", chatId, exp);
            try
            {
                exp = MathHelper.SafeRound(exp * player.ExperienceMultiplier);
                player.Experience += exp;

                var expForNextLevel = CalculateExpForNextLevel(player.Level);

                if (player.Experience >= expForNextLevel)
                {
                    await LevelUp(chatId, player);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"⭐ +{exp} опыта! ({player.Experience}/{expForNextLevel} до следующего уровня)");
                }
            }
            finally
            {
                _logger.LogDebug("AddExperience завершён для chatId {ChatId}", chatId);
            }
        }

        public static int CalculateExpForNextLevel(int currentLevel)
        {
            return MathHelper.SafeRound(100 * Math.Pow(currentLevel, 1.5));
        }

        private async Task LevelUp(long chatId, Player player)
        {
            player.Level++;
            var oldExpRequirement = CalculateExpForNextLevel(player.Level - 1);
            player.Experience = Math.Max(0, player.Experience - oldExpRequirement);

            var healthBonus = MathHelper.SafeRound(20 * (1 + (player.Level - 1) * 0.1));
            var manaBonus = MathHelper.SafeRound(10 * (1 + (player.Level - 1) * 0.05));
            var staminaBonus = MathHelper.SafeRound(5 * (1 + (player.Level - 1) * 0.05));

            player.Health.Max += healthBonus;
            player.Mana.Max += manaBonus;
            player.Stamina.Max += staminaBonus;

            player.Health.Current = player.Health.Max;
            player.Mana.Current = player.Mana.Max;
            player.Stamina.Current = player.Stamina.Max;

            var levelUpText = $@"🎉 *УРОВЕНЬ ПОВЫШЕН!*

⭐ Новый уровень: {player.Level}
❤️ Здоровье: +{healthBonus} ({player.Health.Max})
🔮 Мана: +{manaBonus} ({player.Mana.Max})
💪 Выносливость: +{staminaBonus} ({player.Stamina.Max})";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: levelUpText,
                parseMode: ParseMode.Markdown);

            await ShowLevelUpAnimation(chatId, player.Level);
        }

        private async Task ShowLevelUpAnimation(long chatId, int level)
        {
            var messages = new[]
            {
                "✨ Уровень повышается...",
                $"⭐ Достигнут уровень {level}!",
                "🎉 Новые силы наполняют вас!"
            };

            foreach (var msg in messages)
            {
                var sentMsg = await _botClient.SendTextMessageAsync(chatId, msg);
                await Task.Delay(1000);
                await _botClient.DeleteMessageAsync(chatId, sentMsg.MessageId);
            }
        }

        public async Task HealPlayer(long chatId, Player player, int amount)
        {
            _logger.LogDebug("Начало HealPlayer для chatId {ChatId}, amount {Amount}", chatId, amount);
            try
            {
                amount = MathHelper.Clamp(amount, 1, int.MaxValue);
                player.Health.Add(amount);
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❤️ Восстановлено {amount} здоровья! ({player.Health.Current}/{player.Health.Max})");
            }
            finally
            {
                _logger.LogDebug("HealPlayer завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task RestoreMana(long chatId, Player player, int amount)
        {
            _logger.LogDebug("Начало RestoreMana для chatId {ChatId}, amount {Amount}", chatId, amount);
            try
            {
                amount = MathHelper.Clamp(amount, 1, int.MaxValue);
                player.Mana.Add(amount);
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"🔮 Восстановлено {amount} маны! ({player.Mana.Current}/{player.Mana.Max})");
            }
            finally
            {
                _logger.LogDebug("RestoreMana завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task RestoreStamina(long chatId, Player player, int amount)
        {
            _logger.LogDebug("Начало RestoreStamina для chatId {ChatId}, amount {Amount}", chatId, amount);
            try
            {
                amount = MathHelper.Clamp(amount, 1, int.MaxValue);
                player.Stamina.Add(amount);
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"💪 Восстановлено {amount} выносливости! ({player.Stamina.Current}/{player.Stamina.Max})");
            }
            finally
            {
                _logger.LogDebug("RestoreStamina завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task ProcessRegeneration(long chatId, Player player, TimeSpan timePassed)
        {
            _logger.LogDebug("Начало ProcessRegeneration для chatId {ChatId}", chatId);
            try
            {
                var oldStamina = player.Stamina.Current;
                var oldMana = player.Mana.Current;
                var oldHealth = player.Health.Current;

                player.Stamina.Add(MathHelper.CalculateRegeneration(player.Stamina.Current, player.Stamina.Max, 10, timePassed) - player.Stamina.Current);
                player.Mana.Add(MathHelper.CalculateRegeneration(player.Mana.Current, player.Mana.Max, 5, timePassed) - player.Mana.Current);
                player.Health.Add(MathHelper.CalculateRegeneration(player.Health.Current, player.Health.Max, 2, timePassed) - player.Health.Current);

                if (player.Stamina.Current > oldStamina || player.Mana.Current > oldMana || player.Health.Current > oldHealth)
                {
                    var regenText = "🔄 *Восстановление:*\n";

                    if (player.Stamina.Current > oldStamina)
                        regenText += $"💪 Выносливость: +{player.Stamina.Current - oldStamina}\n";
                    if (player.Mana.Current > oldMana)
                        regenText += $"🔮 Мана: +{player.Mana.Current - oldMana}\n";
                    if (player.Health.Current > oldHealth)
                        regenText += $"❤️ Здоровье: +{player.Health.Current - oldHealth}";

                    await _botClient.SendTextMessageAsync(chatId, regenText);
                }
            }
            finally
            {
                _logger.LogDebug("ProcessRegeneration завершён для chatId {ChatId}", chatId);
            }
        }

        public int CalculateDamage(Player player, int baseDamage, string damageType = "physical")
        {
            _logger.LogDebug("Начало CalculateDamage");
            try
            {
                double multiplier = 1.0;

                switch (damageType.ToLower())
                {
                    case "melee":
                        multiplier = player.MeleeDamageMultiplier;
                        break;
                    case "ranged":
                        multiplier = player.RangedDamageMultiplier;
                        break;
                    case "magic":
                        multiplier = player.MagicDamageMultiplier;
                        break;
                }

                return MathHelper.CalculateDamage(baseDamage, 0, multiplier);
            }
            finally
            {
                _logger.LogDebug("CalculateDamage завершён");
            }
        }

        public int CalculateReceivedDamage(Player player, int incomingDamage)
        {
            _logger.LogDebug("Начало CalculateReceivedDamage");
            try
            {
                var finalDamage = Math.Max(1, incomingDamage - player.Defense);
                return MathHelper.Clamp(finalDamage, 1, player.Health.Current);
            }
            finally
            {
                _logger.LogDebug("CalculateReceivedDamage завершён");
            }
        }
        
    }
}