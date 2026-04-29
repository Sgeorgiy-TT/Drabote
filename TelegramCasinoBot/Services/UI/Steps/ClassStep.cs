using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Data;
using static Player;


namespace TelegramCasinoBot.Services.UI.Steps
{
    public class ClassStep : CreationStepBase
    {
        private readonly IClassService _classService;

        public ClassStep(TelegramBotClient botClient, IClassService classService, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback)
            : base(botClient, CallbackRouter.CLASS, nextStepCallback, restartCallback) 
        {
            _classService = classService;
        }

        public override async Task Ask(long chatId, PlayerBuilder builder)
        {
            var classes = await _classService.GetAllClassesAsync();
            var buttons = classes.Select(cls => new[] { InlineKeyboardButton.WithCallbackData(cls.Name, CreationResponseIdString(cls.Id)) }).ToList();
            var keyboard = new InlineKeyboardMarkup(buttons);
            await _botClient.SendTextMessageAsync(chatId,
                "🎯 *ВЫБОР КЛАССА*\n\nВыберите класс вашего персонажа:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public override async Task Handle(long chatId, PlayerBuilder builder, string data)
        {
            if (!data.StartsWith("class_")) return;
            if (!int.TryParse(data.Substring(6), out int classId)) return;

            var cls = await _classService.GetClassByIdAsync(classId);
            if (cls == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка выбора класса.");
                return;
            }

            builder.SetClass(cls);
            await _nextStepCallback(chatId);
        }

        public override bool CanHandle(string data) => base.CanHandle(data);
    }
}