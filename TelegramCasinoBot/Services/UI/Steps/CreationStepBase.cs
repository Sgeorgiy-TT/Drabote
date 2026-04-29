using System;
using System.Threading.Tasks;
using Telegram.Bot;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public abstract class CreationStepBase : ICreationStep
    {
        protected readonly TelegramBotClient _botClient;
        protected readonly string _key;
        protected readonly Func<long, Task> _nextStepCallback;
        protected readonly Func<long, Task> _restartCallback;

        protected CreationStepBase(TelegramBotClient botClient, string key, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback = null)
        {
            _botClient = botClient;
            _key = key;
            _nextStepCallback = nextStepCallback;
            _restartCallback = restartCallback;
        }

        public abstract Task Ask(long chatId, Player.PlayerBuilder builder);
        public abstract Task Handle(long chatId, Player.PlayerBuilder builder, string data);

        public virtual bool CanHandle(string data) => !string.IsNullOrEmpty(_key) && data.StartsWith(_key);

        protected string CreationResponseIdString(int id) => $"{_key}{id}";
        protected string CreationResponseIdString(string value) => $"{_key}{value}";
    }
}