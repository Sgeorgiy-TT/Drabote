using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Gameplay;

namespace TelegramCasinoBot.Services.UI.Handlers
{
    public class BattleHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly PlayerManager _playerManager;
        private readonly BattleService _battleService;

        public BattleHandler(TelegramBotClient botClient, PlayerManager playerManager, BattleService battleService)
        {
            _botClient = botClient;
            _playerManager = playerManager;
            _battleService = battleService;
        }

        public async Task HandleAttackBoss(long chatId, string data, CallbackQuery callbackQuery)
        {
            //var player = _playerManager.GetPlayer(chatId);
            //if (player != null)
            //    await _battleService.HandleBossBattle(chatId, player, callbackQuery.Message.MessageId);
            //else
            //    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        public async Task HandleDefendBoss(long chatId, string data, CallbackQuery callbackQuery)
        {
            //var player = _playerManager.GetPlayer(chatId);
            //if (player != null)
            //    await _battleService.HandleBossDefense(chatId, player, callbackQuery.Message.MessageId);
            //else
            //    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        public async Task HandleAbilityBoss(long chatId, string data, CallbackQuery callbackQuery)
        {
            //var player = _playerManager.GetPlayer(chatId);
            //if (player != null)
            //    await _battleService.HandleBossAbility(chatId, player, callbackQuery.Message.MessageId);
            //else
            //    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        public async Task HandleFleeBoss(long chatId, string data, CallbackQuery callbackQuery)
        {
            //var player = _playerManager.GetPlayer(chatId);
            //if (player != null)
            //    await _battleService.HandleBossFlee(chatId, player, callbackQuery.Message.MessageId);
            //else
            //    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }
    }
}