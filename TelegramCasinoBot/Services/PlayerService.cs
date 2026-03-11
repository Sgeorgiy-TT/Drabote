using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramMetroidvaniaBot.Utils;

namespace TelegramMetroidvaniaBot.Services
{
    public class PlayerService
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;

        public PlayerService(TelegramBotClient botClient, GameWorld world)
        {
            _botClient = botClient;
            _world = world;
        }

        public async Task AddExperience(long chatId, Player player, int exp)
        {
            // Округляем опыт
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

        public static int CalculateExpForNextLevel(int currentLevel)
        {
            // Формула для опыта до следующего уровня: 100 * уровень^1.5
            return MathHelper.SafeRound(100 * Math.Pow(currentLevel, 1.5));
        }

        private async Task LevelUp(long chatId, Player player)
        {
            player.Level++;
            var oldExpRequirement = CalculateExpForNextLevel(player.Level - 1);
            player.Experience = Math.Max(0, player.Experience - oldExpRequirement);

            // Базовые приросты за уровень
            var healthBonus = MathHelper.SafeRound(20 * (1 + (player.Level - 1) * 0.1));
            var manaBonus = MathHelper.SafeRound(10 * (1 + (player.Level - 1) * 0.05));
            var staminaBonus = MathHelper.SafeRound(5 * (1 + (player.Level - 1) * 0.05));

            player.MaxHealth += healthBonus;
            player.Health = player.MaxHealth;
            player.MaxMana += manaBonus;
            player.Mana = player.MaxMana;
            player.MaxStamina += staminaBonus;
            player.Stamina = player.MaxStamina;

            var levelUpText = $@"🎉 *УРОВЕНЬ ПОВЫШЕН!*

⭐ Новый уровень: {player.Level}
❤️ Здоровье: +{healthBonus} ({player.MaxHealth})
🔮 Мана: +{manaBonus} ({player.MaxMana})
💪 Выносливость: +{staminaBonus} ({player.MaxStamina})";

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: levelUpText,
                parseMode: ParseMode.Markdown);

            // Анимация повышения уровня
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
            amount = MathHelper.Clamp(amount, 1, int.MaxValue);
            player.Health = MathHelper.Clamp(player.Health + amount, 0, player.MaxHealth);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❤️ Восстановлено {amount} здоровья! ({player.Health}/{player.MaxHealth})");
        }

        public async Task RestoreMana(long chatId, Player player, int amount)
        {
            amount = MathHelper.Clamp(amount, 1, int.MaxValue);
            player.Mana = MathHelper.Clamp(player.Mana + amount, 0, player.MaxMana);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"🔮 Восстановлено {amount} маны! ({player.Mana}/{player.MaxMana})");
        }

        public async Task RestoreStamina(long chatId, Player player, int amount)
        {
            amount = MathHelper.Clamp(amount, 1, int.MaxValue);
            player.Stamina = MathHelper.Clamp(player.Stamina + amount, 0, player.MaxStamina);

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"💪 Восстановлено {amount} выносливости! ({player.Stamina}/{player.MaxStamina})");
        }

        /// <summary>
        /// Автоматическое восстановление характеристик со временем
        /// </summary>
        public async Task ProcessRegeneration(long chatId, Player player, TimeSpan timePassed)
        {
            var oldHealth = player.Health;
            var oldMana = player.Mana;
            var oldStamina = player.Stamina;

            // Восстановление: выносливость каждые 30 сек, мана каждые 60 сек, здоровье каждые 120 сек
            player.Stamina = MathHelper.CalculateRegeneration(player.Stamina, player.MaxStamina, 10, timePassed);
            player.Mana = MathHelper.CalculateRegeneration(player.Mana, player.MaxMana, 5, timePassed);
            player.Health = MathHelper.CalculateRegeneration(player.Health, player.MaxHealth, 2, timePassed);

            // Уведомляем если что-то восстановилось
            if (player.Stamina > oldStamina || player.Mana > oldMana || player.Health > oldHealth)
            {
                var regenText = "🔄 *Восстановление:*\n";

                if (player.Stamina > oldStamina)
                    regenText += $"💪 Выносливость: +{player.Stamina - oldStamina}\n";
                if (player.Mana > oldMana)
                    regenText += $"🔮 Мана: +{player.Mana - oldMana}\n";
                if (player.Health > oldHealth)
                    regenText += $"❤️ Здоровье: +{player.Health - oldHealth}";

                await _botClient.SendTextMessageAsync(chatId, regenText);
            }
        }

        /// <summary>
        /// Расчет урона с учетом всех модификаторов
        /// </summary>
        public int CalculateDamage(Player player, int baseDamage, string damageType = "physical")
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

            return MathHelper.CalculateDamage(baseDamage, 0, multiplier); // Защита учитывается при получении урона
        }

        /// <summary>
        /// Расчет получаемого урона с учетом защиты
        /// </summary>
        public int CalculateReceivedDamage(Player player, int incomingDamage)
        {
            // Защита снижает урон: finalDamage = incomingDamage - defense
            var finalDamage = Math.Max(1, incomingDamage - player.Defense);
            return MathHelper.Clamp(finalDamage, 1, player.Health);
        }
    }
}