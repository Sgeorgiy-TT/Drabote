using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static Player;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class NameStep : CreationStepBase
    {
        private PlayerBuilder playerBuilder;

        public NameStep(TelegramBotClient botClient, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback)
            : base(botClient, CallbackRouter.NAME, nextStepCallback, restartCallback) { }

        public override async Task Ask(long chatId, PlayerBuilder builder)
        {
            playerBuilder = builder;
            await _botClient.SendTextMessageAsync(chatId,
                "🎮 *СОЗДАНИЕ ПЕРСОНАЖА*\n\nКак зовут вашего героя?",
                parseMode: ParseMode.Markdown,
                replyMarkup: new ReplyKeyboardRemove());
        }

        public override async Task Handle(long chatId, string data)
        {
            playerBuilder.SetName(data);
            await _nextStepCallback(chatId);
        }

        public override bool CanHandle(string data) => true;
    }
}