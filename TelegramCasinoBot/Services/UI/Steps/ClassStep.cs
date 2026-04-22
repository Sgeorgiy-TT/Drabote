using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Services.Data;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class ClassStep : CreationStepBase
    {
        private readonly IClassService _classService;

        public ClassStep(TelegramBotClient botClient, PlayerCreationUI ui, IClassService classService) : base(botClient, ui)
        {
            _classService = classService;
        }

        public override async Task Ask(long chatId)
        {
            var classes = await _classService.GetAllClassesAsync();
            var buttons = classes.Select(cls => new[] { InlineKeyboardButton.WithCallbackData(cls.Name, $"class_{cls.Id}") }).ToList();
            var keyboard = new InlineKeyboardMarkup(buttons);
            await _botClient.SendTextMessageAsync(chatId,
                "🎯 *ВЫБОР КЛАССА*\n\nВыберите класс вашего персонажа:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public override async Task Handle(long chatId, string data)
        {
            if (!data.StartsWith("class_")) return;
            if (!int.TryParse(data.Substring(6), out int classId)) return;

            var cls = await _classService.GetClassByIdAsync(classId);
            if (cls == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка выбора класса.");
                return;
            }

            _ui.SetClass(chatId, cls);
            await _ui.NextStep(chatId);
        }

        public override bool CanHandle(string data) => data.StartsWith("class_");
    }
}