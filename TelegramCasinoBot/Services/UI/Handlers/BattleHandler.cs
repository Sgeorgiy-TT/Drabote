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

        public async Task HandleMobAction(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player == null)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
                return;
            }
            await _battleService.HandleMobAction(chatId, data, callbackQuery);
        }

        public async Task HandleAbilitySelect(long chatId, string data, CallbackQuery callbackQuery)
        {
            var abilityIdStr = data.Substring("ability_select_".Length);
            if (int.TryParse(abilityIdStr, out int abilityId))
            {
                await _battleService.HandleAbilitySelection(chatId, abilityId, callbackQuery);
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Ошибка выбора способности");
            }
        }

        public async Task HandleItemUse(long chatId, string data, CallbackQuery callbackQuery)
        {
            var itemIdStr = data.Substring("item_use_".Length);
            if (int.TryParse(itemIdStr, out int itemId))
            {
                await _battleService.HandleItemUse(chatId, itemId, callbackQuery);
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Ошибка выбора предмета");
            }
        }

        public async Task HandleBackToBattle(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _battleService.HandleBackToBattle(chatId, callbackQuery);
        }
    }
}