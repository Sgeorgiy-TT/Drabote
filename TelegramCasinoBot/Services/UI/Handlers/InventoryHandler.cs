using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Gameplay;

namespace TelegramCasinoBot.Services.UI.Handlers
{
    public class InventoryHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly PlayerManager _playerManager;
        private readonly InventoryService _inventoryService;
        private readonly GameActionService _gameActionService;

        public InventoryHandler(TelegramBotClient botClient, PlayerManager playerManager, InventoryService inventoryService, GameActionService gameActionService)
        {
            _botClient = botClient;
            _playerManager = playerManager;
            _inventoryService = inventoryService;
            _gameActionService = gameActionService;
        }

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
    }
}