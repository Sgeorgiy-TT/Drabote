using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMetroidvaniaBot.Services
{
    public class BattleService
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly LocationService _locationService;
        private readonly PlayerService _playerService;

        public BattleService(TelegramBotClient botClient, GameWorld world,
                             LocationService locationService = null, PlayerService playerService = null)
        {
            _botClient = botClient;
            _world = world;
            _locationService = locationService ?? new LocationService(botClient, world);
            _playerService = playerService ?? new PlayerService(botClient, world);
        }

        public async Task HandleBossBattle(long chatId, Player player, int messageId)
        {
            if (player.BossHealth <= 0)
                player.BossHealth = 150;

            var rng = new Random();
            var playerDamage = rng.Next(15, 30);
            player.BossHealth -= playerDamage;

            if (player.BossHealth <= 0)
            {
                await HandleBossDefeat(chatId, player, messageId);
                return;
            }

            var bossDamage = rng.Next(10, 20);
            player.Health -= bossDamage;

            var battleText = $@"⚔️ *БИТВА С СТРАЖЕМ ВРАТ*

❤️ Ваше здоровье: {Math.Max(0, player.Health)}/{player.MaxHealth}
👹 Здоровье стража: {Math.Max(0, player.BossHealth)}/150

💥 Вы нанесли {playerDamage} урона!
⚡ Страж атаковал и нанес {bossDamage} урона!";

            if (player.Health <= 0)
            {
                await HandlePlayerDefeat(chatId, player, messageId);
                return;
            }

            await _botClient.EditMessageTextAsync(
                chatId: chatId, messageId: messageId,
                text: battleText, parseMode: ParseMode.Markdown,
                replyMarkup: GetBattleKeyboard());
        }

        public async Task HandleBossDefense(long chatId, Player player, int messageId)
        {
            var bossDamage = new Random().Next(5, 15);
            player.Health -= bossDamage;

            var battleText = $@"🛡️ *БИТВА С СТРАЖЕМ ВРАТ*

❤️ Ваше здоровье: {Math.Max(0, player.Health)}/{player.MaxHealth}
👹 Здоровье стража: {Math.Max(0, player.BossHealth)}/150

🛡️ Вы защитились! Урон снижен.
⚡ Страж атаковал и нанес {bossDamage} урона!";

            await _botClient.EditMessageTextAsync(
                chatId: chatId, messageId: messageId,
                text: battleText, parseMode: ParseMode.Markdown,
                replyMarkup: GetBattleKeyboard());
        }

        public async Task HandleBossAbility(long chatId, Player player, int messageId)
        {
            if (player.Mana < 20)
            {
                await _botClient.AnswerCallbackQueryAsync("", "❌ Недостаточно маны!");
                return;
            }

            var rng = new Random();
            player.Mana -= 20;
            var abilityDamage = rng.Next(25, 40);
            player.BossHealth -= abilityDamage;

            if (player.BossHealth <= 0)
            {
                await HandleBossDefeat(chatId, player, messageId);
                return;
            }

            var bossDamage = rng.Next(10, 20);
            player.Health -= bossDamage;

            var battleText = $@"🔮 *БИТВА С СТРАЖЕМ ВРАТ*

❤️ Ваше здоровье: {Math.Max(0, player.Health)}/{player.MaxHealth}
👹 Здоровье стража: {Math.Max(0, player.BossHealth)}/150
🔮 Мана: {player.Mana}/{player.MaxMana}

✨ Вы использовали Лазерный луч! Нанесено {abilityDamage} урона!
⚡ Страж атаковал и нанес {bossDamage} урона!";

            await _botClient.EditMessageTextAsync(
                chatId: chatId, messageId: messageId,
                text: battleText, parseMode: ParseMode.Markdown,
                replyMarkup: GetBattleKeyboard());
        }

        public async Task HandleBossFlee(long chatId, Player player, int messageId)
        {
            var fleeChance = new Random().Next(0, 100);

            if (fleeChance > 50)
            {
                await _botClient.EditMessageTextAsync(
                    chatId: chatId, messageId: messageId,
                    text: "🏃‍♂️ *ВЫ СМОГЛИ СБЕЖАТЬ!*\n\nВы отступаете к предыдущей локации.",
                    parseMode: ParseMode.Markdown);

                player.CurrentLocation = "crystal_cave";
                player.BossHealth = 0;
                await _locationService.DescribeLocation(chatId, player);
            }
            else
            {
                var bossDamage = new Random().Next(15, 25);
                player.Health -= bossDamage;

                await _botClient.EditMessageTextAsync(
                    chatId: chatId, messageId: messageId,
                    text: $"❌ *НЕУДАЧНАЯ ПОПЫТКА ПОБЕГА!*\n\nСтраж атаковал вас в спину и нанес {bossDamage} урона!",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: GetBattleKeyboard());
            }
        }

        private async Task HandleBossDefeat(long chatId, Player player, int messageId)
        {
            player.BossHealth = 0;
            player.QuestCompleted.Add("defeat_guardian");

            var victoryText = @"🎉 *ПОБЕДА!*

Вы победили Стража Врат! Врата в Святилище Древних теперь открыты.

*Награды:*
⭐ +150 опыта
🔑 Доступ к Святилищу Древних
💪 Новое умение: Сила Древних";

            await _botClient.EditMessageTextAsync(
                chatId: chatId, messageId: messageId,
                text: victoryText, parseMode: ParseMode.Markdown);

            await _playerService.AddExperience(chatId, player, 150);

            if (!player.Abilities.Contains("Сила Древних"))
            {
                player.Abilities.Add("Сила Древних");
                await _botClient.SendTextMessageAsync(chatId, "💪 *Получена новая способность: Сила Древних!*",
                    parseMode: ParseMode.Markdown);
            }
        }

        private async Task HandlePlayerDefeat(long chatId, Player player, int messageId)
        {
            player.Health = player.MaxHealth / 2;
            player.BossHealth = 0;
            player.CurrentLocation = "crystal_cave";

            await _botClient.EditMessageTextAsync(
                chatId: chatId, messageId: messageId,
                text: "💀 *ПОРАЖЕНИЕ!*\n\nСтраж оказался слишком силен. Вы очнулись в Кристальной Пещере.",
                parseMode: ParseMode.Markdown);

            await _locationService.DescribeLocation(chatId, player);
        }

        private static InlineKeyboardMarkup GetBattleKeyboard()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⚔️ Атака", "attack_boss"),
                    InlineKeyboardButton.WithCallbackData("🛡️ Защита", "defend_boss")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔮 Способность", "ability_boss"),
                    InlineKeyboardButton.WithCallbackData("🏃‍♂️ Бегство", "flee_boss")
                }
            });
        }
    }
}
