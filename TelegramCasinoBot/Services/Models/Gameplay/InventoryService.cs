using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Data;

namespace TelegramCasinoBot.Services.Models.Gameplay
{
    public class InventoryService
    {
        private readonly TelegramBotClient _botClient;
        private readonly GameWorld _world;
        private readonly ILogger<InventoryService> _logger;
        private readonly ItemService _itemService;
        public InventoryService(TelegramBotClient botClient, GameWorld world, ItemService itemService, ILogger<InventoryService> logger = null)
        {
            _botClient = botClient;
            _world = world;
            _logger = logger ?? NullLogger<InventoryService>.Instance;
            _itemService = itemService;
        }

        public async Task ShowInteractiveInventory(long chatId, Player player)
        {
            _logger.LogDebug("Начало ShowInteractiveInventory для chatId {ChatId}", chatId);
            try
            {
                var inventoryText = "🎒 *ИНТЕРАКТИВНЫЙ ИНВЕНТАРЬ*\n\n";

                if (player.Inventory.Count > 0)
                {
                    inventoryText += "📦 *Предметы:*\n";

                    var itemButtons = new List<InlineKeyboardButton[]>();
                    foreach (var itemName in player.Inventory)
                    {
                        var item = _itemService.GetItemByName(itemName);
                        var displayName = item != null ? item.GetDisplayName() : itemName;
                        itemButtons.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData($"🎒 {displayName}", $"use_{itemName}"),
                            InlineKeyboardButton.WithCallbackData($"❌ Выбросить", $"drop_{itemName}")
                        });
                    }

                    var keyboard = new InlineKeyboardMarkup(itemButtons);

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: inventoryText,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard);
                }
                else
                {
                    inventoryText += "📭 Инвентарь пуст";
                    await _botClient.SendTextMessageAsync(chatId, inventoryText, parseMode: ParseMode.Markdown);
                }
            }
            finally
            {
                _logger.LogDebug("ShowInteractiveInventory завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task HandleItemPickup(long chatId, Player player, CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало HandleItemPickup для chatId {ChatId}", chatId);
            try
            {
                var location = _world.Locations[player.CurrentLocation];
                var item = callbackQuery.Data.Substring(5);

                if (location.Items.Contains(item))
                {
                    location.Items.Remove(item);
                    player.Inventory.Add(item);

                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"✅ Вы подобрали: {item}");

                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: callbackQuery.Message.MessageId,
                        text: $"*{location.Name}*\n\n{location.Description}\n\n🎁 *Получен предмет:* {item}",
                        parseMode: ParseMode.Markdown);

                    if (item == "Ключ от ворот")
                    {
                        player.AbilityNames.Add("Открытие ворот");
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "🔑 *Ключ от ворот* теперь позволяет открывать запертые врата!",
                            parseMode: ParseMode.Markdown);
                    }
                }
            }
            finally
            {
                _logger.LogDebug("HandleItemPickup завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task HandleItemExamine(long chatId, Player player, CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало HandleItemExamine для chatId {ChatId}", chatId);
            try
            {
                var item = callbackQuery.Data.Substring(8);
                var examination = item switch
                {
                    "Древний артефакт" => "💎 *Древний артефакт*\n\nТаинственный артефакт, испускающий слабое свечение. Похоже, он содержит древнюю силу.",
                    "Ключ от ворот" => "🔑 *Ключ от ворот*\n\nМассивный ключ из бронзы. На нем выгравированы древние символы.",
                    _ => $"📝 {item}\n\nИнтересный предмет, но его назначение не совсем ясно."
                };

                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🔍 Вы осмотрели предмет");
                await _botClient.SendTextMessageAsync(chatId, examination, parseMode: ParseMode.Markdown);
            }
            finally
            {
                _logger.LogDebug("HandleItemExamine завершён для chatId {ChatId}", chatId);
            }
        }
    }
}