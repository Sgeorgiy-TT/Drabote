using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Infrastructure.Location;
using TelegramCasinoBot.Services.Models.Gameplay.Location;

namespace TelegramCasinoBot.Services.UI.Handlers
{
    public class GameMenuHandler
    {
        private readonly TelegramBotClient _botClient;
        private readonly PlayerManager _playerManager;
        private readonly MapService _mapService;
        private readonly LocationService _locationService;
        private readonly MenuServiceTG _menuServiceTG;

        public GameMenuHandler(
            TelegramBotClient botClient,
            PlayerManager playerManager,
            MapService mapService,
            LocationService locationService,
            MenuServiceTG menuServiceTG)
        {
            _botClient = botClient;
            _playerManager = playerManager;
            _mapService = mapService;
            _locationService = locationService;
            _menuServiceTG = menuServiceTG;
        }

        public async Task HandleRefreshMap(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🗺️ Карта обновлена");
                await _mapService.ShowInteractiveMap(chatId, player);
            }
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        public async Task HandleShowLocation(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "📍 Текущая локация");
                await _locationService.DescribeLocation(chatId, player);
            }
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        public async Task HandleSpeedMenu(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _menuServiceTG.ShowSpeedSettings(chatId);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        public async Task HandleSettingsBack(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _menuServiceTG.ShowSettings(chatId);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        public async Task HandleSpeedSelect(long chatId, string data, CallbackQuery callbackQuery)
        {
            var speedStr = data.Substring(6);
            if (int.TryParse(speedStr, out int speed) && speed >= 1 && speed <= 4)
            {
                await _menuServiceTG.SetSpeed(chatId, speed);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Неверное значение скорости");
            }
        }
    }
}