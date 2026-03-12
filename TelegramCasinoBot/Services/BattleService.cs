using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMetroidvaniaBot.Services
{
    public class BattleService
    {
        private readonly ILogger<BattleService> _logger;
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly LocationService _locationService;
        private readonly PlayerService _playerService;

        public BattleService(TelegramBotClient botClient, GameWorld world,
                             LocationService locationService = null, PlayerService playerService = null,
                             ILogger<BattleService> logger = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _botClient = botClient;
            _world = world;
            _locationService = locationService ?? new LocationService(botClient, world);
            _playerService = playerService ?? new PlayerService(botClient, world);
            _logger.LogInformation("BattleService initialized");
        }

        public async Task HandleBossBattle(long chatId, Player player, int messageId)
        {
            _logger.LogDebug("HandleBossBattle called for chatId: {ChatId}", chatId);
            
            if (player.BossHealth <= 0)
                player.BossHealth = 150;

            var rng = new Random();
            var playerDamage = rng.Next(15, 30);
            player.BossHealth -= playerDamage;

            _logger.LogDebug("Player dealt {Damage} damage to boss. Boss health: {Health}", playerDamage, player.BossHealth);

            if (player.BossHealth <= 0)
            {
                _logger.LogInformation("Boss defeated by player in chat {ChatId}", chatId);
                await HandleBossDefeat(chatId, player, messageId);
                return;
            }

            var bossDamage = rng.Next(10, 20);
            player.Health -= bossDamage;

            _logger.LogDebug("Boss dealt {Damage} damage to player. Player health: {Health}", bossDamage, player.Health);

            var battleText = $@"⚔️ *БИТВА С СТРАЖЕМ ВРАТ*

❤️ Ваше здоровье: {Math.Max(0, player.Health)}/{player.MaxHealth}
👹 Здоровье стража: {Math.Max(0, player.BossHealth)}/150

💥 Вы нанесли {playerDamage} урона!
⚡ Страж атаковал и нанес {bossDamage} урона!";

            if (player.Health <= 0)
            {
                _logger.LogWarning("Player defeated by boss in chat {ChatId}", chatId);
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
            _logger.LogDebug("HandleBossDefense called for chatId: {ChatId}", chatId);
            
            var bossDamage = new Random().Next(5, 15);
            player.Health -= bossDamage;

            _logger.LogDebug("Boss dealt {Damage} damage while player defended", bossDamage);

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
            _logger.LogDebug("HandleBossAbility called for chatId: {ChatId}", chatId);
            
            if (player.Mana < 20)
            {
                _logger.LogWarning("Player attempted to use ability without enough mana. Mana: {Mana}", player.Mana);
                await _botClient.AnswerCallbackQueryAsync("", "❌ Недостаточно маны!");
                return;
            }

            var rng = new Random();
            player.Mana -= 20;
            var abilityDamage = rng.Next(25, 40);
            player.BossHealth -= abilityDamage;

            _logger.LogDebug("Player used ability dealing {Damage} damage. Boss health: {Health}", abilityDamage, player.BossHealth);

            if (player.BossHealth <= 0)
            {
                _logger.LogInformation("Boss defeated by player ability in chat {ChatId}", chatId);
                await HandleBossDefeat(chatId, player, messageId);
                return;
            }

            var bossDamage = rng.Next(10, 20);
            player.Health -= bossDamage;

            _logger.LogDebug("Boss dealt {Damage} damage after player ability", bossDamage);

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
            _logger.LogDebug("HandleBossFlee called for chatId: {ChatId}", chatId);
            
            var fleeChance = new Random().Next(0, 100);

            if (fleeChance > 50)
            {
                _logger.LogInformation("Player successfully fled from boss in chat {ChatId}", chatId);
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
                _logger.LogWarning("Player failed to flee from boss in chat {ChatId}", chatId);
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
            _logger.LogInformation("Boss defeated in chat {ChatId}", chatId);
            
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
            _logger.LogWarning("Player defeated in chat {ChatId}. Respawning at crystal_cave", chatId);
            
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
