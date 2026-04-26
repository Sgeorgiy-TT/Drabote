using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.UI.Steps;
namespace TelegramCasinoBot.Services.UI.Steps
{
    public class GenderStep : CreationStepBase
    {
        public GenderStep(TelegramBotClient botClient, PlayerCreationUI ui)
        : base(botClient, ui, CallbackRouter.GENDER) { }
        public override async Task Ask(long chatId)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "👨 Мужской", "👩 Женский" },
                new KeyboardButton[] { "🔙 Назад" }
            })
            { ResizeKeyboard = true };
            await _botClient.SendTextMessageAsync(chatId, "Выберите пол вашего персонажа:", replyMarkup: keyboard);
        }

        public override async Task Handle(long chatId, string data)
        {
            string selected = data.Contains("Мужской") ? "Male" : data.Contains("Женский") ? "Female" : null;
            if (selected != null)
            {
                _ui.SetGender(chatId, selected);
                await _ui.NextStep(chatId);
            }
            else
            {
                await Ask(chatId);
            }
        }

        public override bool CanHandle(string data) => data.Contains("Мужской") || data.Contains("Женский") || data == "🔙 Назад";
    }
}