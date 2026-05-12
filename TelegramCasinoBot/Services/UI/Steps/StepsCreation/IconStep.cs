using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramCasinoBot.Services.Infrastructure;
using static Player;

namespace TelegramCasinoBot.Services.UI.Steps.StepsCreation
{
    public class IconStep : CreationStepBase
    {
        public const string CONFIRM_ICON = "confirm_icon";
        public const string SELECT_ICON = "select_icon_";
        public const string ICONS_PREV = "icons_prev";
        public const string ICONS_NEXT = "icons_next";
        public const string PREVIEW_ALL = "preview_all";
        private readonly CharacterIconService _iconService;
        private PlayerBuilder playerBuilder;
        private readonly ILogger<IconStep> _logger;

        public IconStep(TelegramBotClient botClient, CharacterIconService iconService, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback, ILogger<IconStep> logger)
            : base(botClient, SELECT_ICON, nextStepCallback, restartCallback)
        {
            _iconService = iconService;
            _logger = logger;
        }

        public override async Task Ask(long chatId, PlayerBuilder builder)
        {
            playerBuilder = builder;
            var gender = builder.GetGender();
            var race = builder.GetRace()?.Name;
            _logger.LogDebug("[{ChatId}] IconStep.Ask: гендер='{Gender}', раса='{Race}', билдер Hash={Hash}", chatId, gender, race, builder.GetHashCode());
            await _botClient.SendTextMessageAsync(chatId, "🎨 Теперь выберите внешность вашего персонажа!", parseMode: ParseMode.Markdown);
            if (string.IsNullOrEmpty(gender) || string.IsNullOrEmpty(race))
            {
                _logger.LogWarning("[{ChatId}] Не выбран пол или раса (гендер='{Gender}', раса='{Race}'). Перезапуск...", chatId, gender, race);
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка: не выбран пол или раса. Начните создание заново.");
                await _restartCallback(chatId);
                return;
            }
            await _iconService.StartIconSelection(chatId, gender, race);
        }

        public override async Task Handle(long chatId, PlayerBuilder builder, string data)
        {

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

        public override bool CanHandle(string data) => data.StartsWith("select_icon") || data == "confirm_icon" || data == "change_icon" || data == "icons_prev" || data == "icons_next" || data == "preview_all";
    }
}