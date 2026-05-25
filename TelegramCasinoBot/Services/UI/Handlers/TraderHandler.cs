using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Data.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay;

namespace TelegramCasinoBot.Services.UI.Handlers
{
    public class TraderHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly PlayerManager _playerManager;
        private readonly ShopService _shopService;
        private readonly ItemService _itemService;
        private readonly AbilityService _abilityService;
        private readonly QuestService _questService;
        private readonly ILogger<TraderHandler> _logger;
        private readonly DatabaseService _databaseService;
        public TraderHandler(
            TelegramBotClient botClient,
            PlayerManager playerManager,
            ShopService shopService,
            ItemService itemService,
            AbilityService abilityService,
            QuestService questService, ILogger<TraderHandler> logger, DatabaseService databaseService)
        {
            _botClient = botClient;
            _playerManager = playerManager;
            _shopService = shopService;
            _itemService = itemService;
            _abilityService = abilityService;
            _questService = questService;
            _logger = logger;
            _databaseService = databaseService;
        }

        public async Task ShowTraderMenu(long chatId, Player player)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🛒 Купить предметы", "trader_items") },
                new[] { InlineKeyboardButton.WithCallbackData("✨ Купить способности", "trader_abilities") },
                new[] { InlineKeyboardButton.WithCallbackData("📜 Квесты", "trader_quests") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "back_to_game") }
            });
            await _botClient.SendTextMessageAsync(chatId, "🏪 *Торговец*\nЧем могу помочь?", parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        public async Task HandleTraderItems(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;

            var shopItems = _shopService.GetAvailableItems(player.Level);
            if (!shopItems.Any())
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Нет доступных предметов");
                await ShowTraderMenu(chatId, player);
                return;
            }

            var buttons = shopItems.Select(i =>
            {
                var displayName = i.Item.GetDisplayName();
                return new[] { InlineKeyboardButton.WithCallbackData($"{displayName} - {i.Price}💰", $"buy_item_{i.Item.Id}") };
            }).ToList();
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "trader_back") });

            await _botClient.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
            await _botClient.SendTextMessageAsync(chatId, "🛒 *Выберите предмет для покупки:*",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(buttons));
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
  
        }

        public async Task HandleTraderAbilities(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;

            var shopAbilities = _shopService.GetAvailableAbilities(player.Level);
            if (!shopAbilities.Any())
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Нет доступных способностей");
                await ShowTraderMenu(chatId, player);
                return;
            }

            var buttons = shopAbilities.Select(a =>
            {
                return new[] { InlineKeyboardButton.WithCallbackData($"{a.Ability.Name} - {a.Price}💰", $"buy_ability_{a.Ability.Id}") };
            }).ToList();
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "trader_back") });

            await _botClient.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
            await _botClient.SendTextMessageAsync(chatId, "✨ *Выберите способность для покупки:*",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(buttons));
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
   
        }

        public async Task HandleBuyItem(long chatId, string data, CallbackQuery callbackQuery)
        {
            var itemId = int.Parse(data.Substring("buy_item_".Length));
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;

            var shopItem = _shopService.GetAvailableItems(player.Level).FirstOrDefault(i => i.Item.Id == itemId);
            if (shopItem.Item == null)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Предмет недоступен");
                return;
            }
            if (player.Gold < shopItem.Price)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Недостаточно золота");
                return;
            }
            var item = _itemService.GetItemById(itemId);
            if (item == null) return;

            player.Gold -= shopItem.Price;
            player.Inventory.Add(item.Name);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"✅ Вы купили {item.GetDisplayName()} за {shopItem.Price}💰");
            await HandleTraderItems(chatId, data, callbackQuery);
            await _databaseService.SavePlayerAsync(player);
        }

        public async Task HandleBuyAbility(long chatId, string data, CallbackQuery callbackQuery)
        {
            var abilityId = int.Parse(data.Substring("buy_ability_".Length));
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;

            var shopAbility = _shopService.GetAvailableAbilities(player.Level).FirstOrDefault(a => a.Ability.Id == abilityId);
            if (shopAbility.Ability == null)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Способность недоступна");
                return;
            }
            if (player.Gold < shopAbility.Price)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Недостаточно золота");
                return;
            }
            var ability = _abilityService.GetAbilityById(abilityId);
            if (ability == null) return;

            player.Gold -= shopAbility.Price;
            player.LearnedAbilities.Add(ability);
            player.AbilityNames.Add(ability.Name);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"✅ Вы купили способность {ability.Name} за {shopAbility.Price}💰");
            await HandleTraderAbilities(chatId, data, callbackQuery);
            await _databaseService.SavePlayerAsync(player);
        }

        public async Task HandleTraderQuests(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;

            var quest = _questService.GetQuestById(player.CurrentQuestId);
            if (quest == null)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Нет активных квестов");
                await ShowTraderMenu(chatId, player);
                return;
            }

            var progress = player.QuestProgress.FirstOrDefault(p => p.QuestId == quest.Id);
            var current = progress?.CurrentCount ?? 0;
            var text = $"📜 *{quest.Name}*\n{quest.Description}\nПрогресс: {current}/{quest.RequiredCount}\nНаграда: +{quest.RewardExp} опыта, {quest.RewardGold}💰";
            var keyboard = new InlineKeyboardMarkup(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "trader_back") });

            await _botClient.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
            await _botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            _logger.LogDebug($"Quest {quest.Id}: progress from memory = {current}, expected from Player.QuestProgress = {player.QuestProgress.FirstOrDefault(p => p.QuestId == quest.Id)?.CurrentCount}");
       
        }

        public async Task HandleTraderBack(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
            {
                await _botClient.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
                await ShowTraderMenu(chatId, player);
            }
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }
    }
}