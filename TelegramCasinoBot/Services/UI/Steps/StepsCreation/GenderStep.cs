using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using static Player;

namespace TelegramCasinoBot.Services.UI.Steps.StepsCreation
{
    public class GenderStep : CreationStepBase
    {
        public const string GENDER = "gender";
        private readonly ILogger<GenderStep> _logger;

        public GenderStep(TelegramBotClient botClient, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback, Func<long, Task> goBackCallback, ILogger<GenderStep> logger)
            : base(botClient, GENDER, nextStepCallback, restartCallback, goBackCallback)
        {
            _logger = logger;
        }

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
            _logger.LogDebug("[{ChatId}] GenderStep.Handle: data='{Data}'", chatId, data);

            if (data == "back")
            {
                if (_goBackCallback != null)
                    await _goBackCallback(chatId);
                return;
            }

            if (data.Contains("Мужской"))
            {
                builder.SetGender("Male");
                _logger.LogDebug("[{ChatId}] Установлен пол 'Male'", chatId);
            }
            else if (data.Contains("Женский"))
            {
                builder.SetGender("Female");
                _logger.LogDebug("[{ChatId}] Установлен пол 'Female'", chatId);
            }
            else
            {
                _logger.LogWarning("[{ChatId}] Неизвестные данные пола: {Data}", chatId, data);
                await Ask(chatId, builder);
                return;
            }
            await _nextStepCallback(chatId);
        }

        public override bool CanHandle(string data) => data.Contains("Мужской") || data.Contains("Женский") || data == "back";
    }
}