using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramCasinoBot.Services.UI.Steps.Dispatcher
{
    public class CallbackDispatcher
    {
        private readonly List<(string Key, Func<long, string, CallbackQuery, Task> Handler, bool IsPrefix)> _handlers;
        private readonly TelegramBotClient _botClient;

        public CallbackDispatcher(TelegramBotClient botClient)
        {
            _botClient = botClient;
            _handlers = new List<(string, Func<long, string, CallbackQuery, Task>, bool)>();
        }

        public void Register(string key, Func<long, string, CallbackQuery, Task> handler, bool isPrefix = false)
        {
            _handlers.Add((key, handler, isPrefix));
        }

        public async Task DispatchAsync(long chatId, string data, CallbackQuery callbackQuery)
        {
            foreach (var (key, handler, isPrefix) in _handlers)
            {
                if (isPrefix ? data.StartsWith(key) : data == key)
                {
                    await handler(chatId, data, callbackQuery);
                    return;
                }
            }
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Неизвестное действие");
        }
    }
}