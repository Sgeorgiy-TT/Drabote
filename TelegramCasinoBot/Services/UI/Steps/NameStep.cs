using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class NameStep : CreationStepBase
    {
        public NameStep(TelegramBotClient botClient, PlayerCreationUI ui) : base(botClient, ui) { }

        public override async Task Ask(long chatId)
        {
            await _botClient.SendTextMessageAsync(chatId,
                "🎮 *СОЗДАНИЕ ПЕРСОНАЖА*\n\nКак зовут вашего героя?",
                parseMode: ParseMode.Markdown,
                replyMarkup: new ReplyKeyboardRemove());
        }

        public override async Task Handle(long chatId, string data)
        {
            _ui.SetName(chatId, data);
            await _ui.NextStep(chatId);
        }

        public override bool CanHandle(string data) => true;
    }
}