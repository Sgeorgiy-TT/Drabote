using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class SummaryStep : CreationStepBase
    {
        public SummaryStep(TelegramBotClient botClient, PlayerCreationUI ui)
        : base(botClient, ui, CallbackRouter.CONFIRM_CHARACTER){ }
            
        public override async Task Ask(long chatId)
        {
            var tempPlayer = _ui.GetTempPlayer(chatId);
            var summary = $@"🎉 *ПЕРСОНАЖ СОЗДАН!*

            *Имя:* {tempPlayer.Name}
            *Пол:* {(tempPlayer.Gender == "Male" ? "👨 Мужской" : "👩 Женский")}
            *Раса:* {tempPlayer.Race}
            *Класс:* {tempPlayer.Class}

            *Характеристики:*
            ❤️ Здоровье: {tempPlayer.Health.Current}/{tempPlayer.Health.Max}
            🔮 Мана: {tempPlayer.Mana.Current}/{tempPlayer.Mana.Max}
            💪 Выносливость: {tempPlayer.Stamina.Current}/{tempPlayer.Stamina.Max}
            🛡️ Защита: {tempPlayer.Defense}

            *Бонусы:*
            ⭐ Множитель опыта: {Math.Round(tempPlayer.ExperienceMultiplier * 100, 1)}%
            ⚔️ Ближний урон: {Math.Round(tempPlayer.MeleeDamageMultiplier * 100, 1)}%
            🏹 Дальний урон: {Math.Round(tempPlayer.RangedDamageMultiplier * 100, 1)}%
            🔮 Магический урон: {Math.Round(tempPlayer.MagicDamageMultiplier * 100, 1)}%

            *Способности:* {string.Join(", ", tempPlayer.Abilities)}

            Готовы начать приключение?";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✅ Начать игру", "confirm_character") },
                new[] { InlineKeyboardButton.WithCallbackData("🔁 Пересоздать", "restart_character") }
            });
            await _botClient.SendTextMessageAsync(chatId, summary, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        public override async Task Handle(long chatId, string data)
        {
            if (data == "confirm_character")
            {
                await _ui.CompleteCreation(chatId);
            }
            else if (data == "restart_character")
            {
                _ui.StartCreation(chatId);
            }
        }

        public override bool CanHandle(string data) => data == "confirm_character" || data == "restart_character";
    }
}