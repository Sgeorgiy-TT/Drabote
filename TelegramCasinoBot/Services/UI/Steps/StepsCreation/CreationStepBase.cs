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
        protected readonly Func<long, Task> _goBackCallback;

        public string CallbackKey => _key;

        protected CreationStepBase(
            TelegramBotClient botClient, 
            string key, 
            Func<long, Task> nextStepCallback, 
            Func<long, Task> restartCallback = null, 
            Func<long, Task> goBackCallback = null)
        {
            _botClient = botClient;
            _key = key;
            _nextStepCallback = nextStepCallback;
            _restartCallback = restartCallback;
            _goBackCallback = goBackCallback;
        }

        public abstract Task Ask(long chatId, Player.PlayerBuilder builder);
        public abstract Task Handle(long chatId, Player.PlayerBuilder builder, string data);

        public virtual bool CanHandle(string data) => !string.IsNullOrEmpty(_key) && data.StartsWith(_key);

        protected string CreationResponseIdString(int id) => $"{_key}{id}";
    }
}