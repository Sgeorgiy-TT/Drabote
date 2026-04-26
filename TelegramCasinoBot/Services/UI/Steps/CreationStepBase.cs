using System.Diagnostics;
using System.Threading.Tasks;
using Telegram.Bot;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public abstract class CreationStepBase : ICreationStep
    {
        protected readonly TelegramBotClient _botClient;
        protected readonly PlayerCreationUI _ui;
        protected readonly string _key;
        
        protected CreationStepBase(TelegramBotClient botClient, PlayerCreationUI ui, string key)
        {
            _botClient = botClient;
            _ui = ui;
            _key = key;
        }
        
        public abstract Task Ask(long chatId);
        public abstract Task Handle(long chatId, string data);
        public virtual bool CanHandle(string data) => !string.IsNullOrEmpty(_key) && data.StartsWith(_key);
        protected string CreationResponseIdString(int id) => $"{_key}{id}";
        protected string CreationResponseIdString(string value) => $"{_key}{value}";
    }
}