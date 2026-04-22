using System.Threading.Tasks;
using Telegram.Bot;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public abstract class CreationStepBase : ICreationStep
    {
        protected readonly TelegramBotClient _botClient;
        protected readonly PlayerCreationUI _ui;
        public const string _key;
        protected CreationStepBase(TelegramBotClient botClient, PlayerCreationUI ui, string key)
        {
            _botClient = botClient;
            _ui = ui;
            _key = key;
        }
        
        public abstract Task Ask(long chatId);//chatId можно сохранить в поле
        public abstract Task Handle(long chatId, string data);
        public abstract bool CanHandle(string data);
    }
}