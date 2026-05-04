using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Gameplay;

namespace TelegramCasinoBot.Services.UI.Handlers
{
    public class SpecialActionsHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly PlayerManager _playerManager;
        private readonly GameActionService _gameActionService;

        public SpecialActionsHandler(TelegramBotClient botClient, PlayerManager playerManager, GameActionService gameActionService)
        {
            _botClient = botClient;
            _playerManager = playerManager;
            _gameActionService = gameActionService;
        }

        public async Task HandleLearnLaser(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _gameActionService.LearnLaserAbility(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        public async Task HandleAttackCrystal(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _gameActionService.AttackCrystal(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }
    }
}