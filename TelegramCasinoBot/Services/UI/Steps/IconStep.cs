using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Infrastructure;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class IconStep : CreationStepBase
    {
        private readonly CharacterIconService _iconService;

        public IconStep(TelegramBotClient botClient, PlayerCreationUI ui, CharacterIconService iconService) : base(botClient, ui)
        {
            _iconService = iconService;
        }

        public override async Task Ask(long chatId)
        {
            await _botClient.SendTextMessageAsync(chatId, "🎨 Теперь выберите внешность вашего персонажа!", parseMode: ParseMode.Markdown);
            var gender = _ui.GetGender(chatId);
            var raceName = _ui.GetRaceName(chatId);
            await _iconService.StartIconSelection(chatId, gender, raceName);
        }

        public override async Task Handle(long chatId, string data)
        {
            if (data == "confirm_icon")
            {
                var iconPath = _iconService.GetSelectedIconPath(chatId);
                if (!string.IsNullOrEmpty(iconPath))
                {
                    _ui.SetIconPath(chatId, iconPath);
                    await _ui.NextStep(chatId);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Иконка не выбрана. Пожалуйста, выберите иконку.");
                    await Ask(chatId);
                }
                _iconService.ClearSelection(chatId);
            }
            else if (data == "change_icon")
            {
                await Ask(chatId);
            }
            else
            {
                await _iconService.HandleIconSelection(chatId, data);
            }
        }

        public override bool CanHandle(string data) => data.StartsWith("select_icon_") || data == "confirm_icon" || data == "change_icon" || data == "icons_prev" || data == "icons_next" || data == "preview_all";
    }
}