using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using static Player;

namespace TelegramCasinoBot.Services.UI.Steps.StepsCreation
{
    public class IconStep : CreationStepBase
    {
        public const string SELECT_ICON = "select_icon_";
        private readonly CharacterIconService _iconService;
        private readonly ILogger<IconStep> _logger;

        public IconStep(TelegramBotClient botClient, CharacterIconService iconService, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback, Func<long, Task> goBackCallback, ILogger<IconStep> logger)
            : base(botClient, SELECT_ICON, nextStepCallback, restartCallback, goBackCallback)
        {
            _iconService = iconService;
            _logger = logger;
        }

        public override async Task Ask(long chatId, PlayerBuilder builder)
        {
            var gender = builder.GetGender();
            var race = builder.GetRace()?.Name;
            _logger.LogDebug("[{ChatId}] IconStep.Ask: гендер='{Gender}', раса='{Race}'", chatId, gender, race);
            await _botClient.SendTextMessageAsync(chatId, "🎨 Теперь выберите внешность вашего персонажа!", parseMode: ParseMode.Markdown);
            await _iconService.StartIconSelection(chatId, gender, race, _goBackCallback);
        }

        public override async Task Handle(long chatId, PlayerBuilder builder, string data)
        {
            if (data == "back")
            {
                if (_goBackCallback != null)
                    await _goBackCallback(chatId);
                return;
            }

            if (data == "confirm_icon")
            {
                var iconPath = _iconService.GetSelectedIconPath(chatId);
                if (!string.IsNullOrEmpty(iconPath))
                {
                    builder.SetIconName(iconPath);
                    await _nextStepCallback(chatId);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Иконка не выбрана.");
                    await Ask(chatId, builder);
                }
                _iconService.ClearSelection(chatId);
            }
            else if (data == "change_icon")
            {
                await Ask(chatId, builder);
            }
            else
            {
                await _iconService.HandleIconSelection(chatId, data);
            }
        }

        public override bool CanHandle(string data) => data.StartsWith("select_icon") || data == "confirm_icon" || data == "change_icon" || data == "icons_prev" || data == "icons_next" || data == "preview_all" || data == "back";
    }
}