using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Gameplay;
using TelegramCasinoBot.Services.Data;

namespace TelegramCasinoBot.Services.UI.Handlers
{
    public class InventoryHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly PlayerManager _playerManager;
        private readonly InventoryService _inventoryService;
        private readonly GameActionService _gameActionService;
        private readonly ItemService _itemService;

        public InventoryHandler(TelegramBotClient botClient, PlayerManager playerManager, InventoryService inventoryService, GameActionService gameActionService, ItemService itemService)
        {
            _botClient = botClient;
            _playerManager = playerManager;
            _inventoryService = inventoryService;
            _gameActionService = gameActionService;
            _itemService = itemService;
        }

        // ========== СТАРЫЕ МЕТОДЫ (Take, Examine, Use, Drop) ==========
        public async Task HandleTake(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _inventoryService.HandleItemPickup(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        public async Task HandleExamine(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _inventoryService.HandleItemExamine(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        public async Task HandleUse(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _gameActionService.UseItem(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        public async Task HandleDrop(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _gameActionService.DropItem(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        // ========== НОВЫЕ МЕТОДЫ ЭКИПИРОВКИ ==========
        public async Task HandleEquipmentMenu(long chatId, string data, CallbackQuery callbackQuery)
        {
            await ShowEquipmentMenu(chatId, _playerManager.GetPlayer(chatId));
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        public async Task ShowEquipmentMenu(long chatId, Player player)
        {
            if (player == null) return;
            var weapon = player.EquippedWeaponId != 0 ? _itemService.GetItemById(player.EquippedWeaponId) : null;
            var armor = player.EquippedArmorId != 0 ? _itemService.GetItemById(player.EquippedArmorId) : null;
            var text = $"🎽 *ЭКИПИРОВКА*\n\n" +
                       $"⚔️ Оружие: {(weapon != null ? weapon.Name : "нет")}\n" +
                       $"🛡️ Броня: {(armor != null ? armor.Name : "нет")}\n\n" +
                       $"Выберите действие:";
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⚔️ Экипировать оружие", "equip_weapon") },
                new[] { InlineKeyboardButton.WithCallbackData("🛡️ Экипировать броню", "equip_armor") },
                new[] { InlineKeyboardButton.WithCallbackData("🗑️ Снять оружие", "unequip_weapon") },
                new[] { InlineKeyboardButton.WithCallbackData("🗑️ Снять броню", "unequip_armor") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "equipment_back") }
            });
            await _botClient.SendTextMessageAsync(chatId, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: keyboard);
        }

        public async Task HandleEquipWeapon(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;
            var weapons = player.Inventory
                .Select(name => _itemService.GetItemByName(name))
                .Where(i => i != null && i.ItemType == "weapon")
                .ToList();
            if (weapons.Count == 0)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Нет оружия в инвентаре");
                return;
            }
            var buttons = weapons.Select(w => new[] { InlineKeyboardButton.WithCallbackData(w.Name, $"equip_select_{w.Id}") }).ToList();
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "equipment_back") });
            await _botClient.EditMessageTextAsync(chatId, callbackQuery.Message.MessageId, "🎽 *Выберите оружие:*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: new InlineKeyboardMarkup(buttons));
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        public async Task HandleEquipArmor(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;
            var armors = player.Inventory
                .Select(name => _itemService.GetItemByName(name))
                .Where(i => i != null && i.ItemType == "armor")
                .ToList();
            if (armors.Count == 0)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Нет брони в инвентаре");
                return;
            }
            var buttons = armors.Select(a => new[] { InlineKeyboardButton.WithCallbackData(a.Name, $"equip_select_{a.Id}") }).ToList();
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "equipment_back") });
            await _botClient.EditMessageTextAsync(chatId, callbackQuery.Message.MessageId, "🎽 *Выберите броню:*", parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: new InlineKeyboardMarkup(buttons));
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        public async Task HandleEquipSelect(long chatId, string data, CallbackQuery callbackQuery)
        {
            var itemId = int.Parse(data.Substring("equip_select_".Length));
            var item = _itemService.GetItemById(itemId);
            if (item == null) return;
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;
            if (player.EquipItem(item, _itemService))
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"✅ {item.Name} экипировано!");
                await ShowEquipmentMenu(chatId, player);
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Не удалось экипировать");
            }
        }

        public async Task HandleUnequipWeapon(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;
            if (player.UnequipItem("weapon"))
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Оружие снято");
                await ShowEquipmentMenu(chatId, player);
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Нечего снимать");
            }
        }

        public async Task HandleUnequipArmor(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;
            if (player.UnequipItem("armor"))
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Броня снята");
                await ShowEquipmentMenu(chatId, player);
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Нечего снимать");
            }
        }

        public async Task HandleEquipmentBack(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await ShowEquipmentMenu(chatId, player);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }
    }
}