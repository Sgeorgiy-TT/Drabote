using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Models.Data.Creation;
using static Player;


namespace TelegramCasinoBot.Services.UI.Steps.StepsCreation
{
    public class ClassStep : CreationStepBase
    {
        public const string CLASS = "class_";
        private readonly IClassService _classService;
        private PlayerBuilder playerBuilder;
        private readonly ILogger<ClassStep> _logger;
        public ClassStep(TelegramBotClient botClient, IClassService classService, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback, ILogger<ClassStep> logger)
            : base(botClient, CLASS, nextStepCallback, restartCallback) 
        {
            _classService = classService;
            _logger = logger;
        }

        public override async Task Ask(long chatId, PlayerBuilder builder)
        {
            var classes = await _classService.GetAllClassesAsync();
            var buttons = classes.Select(cls => new[] { InlineKeyboardButton.WithCallbackData(cls.Name, CreationResponseIdString(cls.Id)) }).ToList();
            playerBuilder = builder;
            var keyboard = new InlineKeyboardMarkup(buttons);
            await _botClient.SendTextMessageAsync(chatId,
                "🎯 *ВЫБОР КЛАССА*\n\nВыберите класс вашего персонажа:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public override async Task Handle(long chatId, PlayerBuilder builder, string data)
        {
            _logger.LogDebug("[{ChatId}] ClassStep.Handle: data='{Data}', билдер Hash={Hash}", chatId, data, builder.GetHashCode());
            if (!data.StartsWith("class_")) return;
            if (!int.TryParse(data.Substring(6), out int classId)) return;
            var cls = await _classService.GetClassByIdAsync(classId);
            if (cls == null)
            {
                _logger.LogWarning("[{ChatId}] Класс с Id {ClassId} не найден", chatId, classId);
                return;
            }
            builder.SetClass(cls);
            _logger.LogDebug("[{ChatId}] Установлен класс '{ClassName}'", chatId, cls.Name);
            await _nextStepCallback(chatId);
        }
        public override bool CanHandle(string data) => base.CanHandle(data);
    }
}