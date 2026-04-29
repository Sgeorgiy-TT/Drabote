using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using static Player;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class GenderStep : CreationStepBase
    {
        public GenderStep(TelegramBotClient botClient, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback)
            : base(botClient, CallbackRouter.GENDER, nextStepCallback, restartCallback) { }

        public override async Task Ask(long chatId, PlayerBuilder builder)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "👨 Мужской", "👩 Женский" },
                new KeyboardButton[] { "🔙 Назад" }
            })
            { ResizeKeyboard = true };
            await _botClient.SendTextMessageAsync(chatId, "Выберите пол вашего персонажа:", replyMarkup: keyboard);
        }

        public override async Task Handle(long chatId, PlayerBuilder builder, string data)
        {
            string selected = data.Contains("Мужской") ? "Male" : data.Contains("Женский") ? "Female" : null;
            if (selected != null)
            {
                builder.SetGender(selected);
                await _nextStepCallback(chatId);
            }
            else
            {
                await Ask(chatId, builder);
            }
        }

        public override bool CanHandle(string data) => data.Contains("Мужской") || data.Contains("Женский") || data == "🔙 Назад";
    }
}