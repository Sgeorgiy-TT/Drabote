using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelegramCasinoBot.Services.Infrastructure;
using static Player;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class IconStep : CreationStepBase
    {
        private readonly CharacterIconService _iconService;
        private PlayerBuilder playerBuilder;

        public IconStep(TelegramBotClient botClient, CharacterIconService iconService, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback)
            : base(botClient, CallbackRouter.SELECT_ICON, nextStepCallback, restartCallback)
        {
            _iconService = iconService;
        }

        public override async Task Ask(long chatId, PlayerBuilder builder)
        {
            playerBuilder = builder;
            await _botClient.SendTextMessageAsync(chatId, "🎨 Теперь выберите внешность вашего персонажа!", parseMode: ParseMode.Markdown);
            var gender = builder.GetGender();
            var race = builder.GetRace();
            var raceName = race?.Name;
            await _iconService.StartIconSelection(chatId, gender, raceName);
        }

        public override async Task Handle(long chatId, string data)
        {
            if (data == "confirm_icon")
            {
                var iconPath = _iconService.GetSelectedIconPath(chatId);
                if (!string.IsNullOrEmpty(iconPath))
                {
                    playerBuilder.SetIconName(iconPath);
                    await _nextStepCallback(chatId);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Иконка не выбрана. Пожалуйста, выберите иконку.");
                    await Ask(chatId, playerBuilder);
                }
                _iconService.ClearSelection(chatId);
            }
            else if (data == "change_icon")
            {
                await Ask(chatId, playerBuilder);
            }
            else
            {
                await _iconService.HandleIconSelection(chatId, data);
            }
        }

        public override bool CanHandle(string data) => data.StartsWith("select_icon") || data == "confirm_icon" || data == "change_icon" || data == "icons_prev" || data == "icons_next" || data == "preview_all";
    }
}