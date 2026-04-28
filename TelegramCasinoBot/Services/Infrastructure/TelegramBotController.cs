using System;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;

namespace TelegramCasinoBot.Services.Infrastructure
{
    public class TelegramBotController
    {
        private readonly string _token;
        private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        private TelegramBotClient _botClient;

        public TelegramBotController(string token)
        {
            _token = token;
            _botClient = new TelegramBotClient(_token, _httpClient);
        }

        public TelegramBotClient Client => _botClient;

        public async Task<bool> ReconnectAsync()
        {
            try
            {
                _botClient = new TelegramBotClient(_token, _httpClient);
                await _botClient.GetMeAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}