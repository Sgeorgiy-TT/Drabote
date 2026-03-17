using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMetroidvaniaBot.Services
{
    public static class KeyboardHelper
    {
        public static ReplyKeyboardMarkup GetEnhancedControls()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "⬆️ Север", "⬇️ Юг" },
                new KeyboardButton[] { "⬅️ Запад", "➡️ Восток" },
                new KeyboardButton[] { "🗺️ Карта", "🎒 Инвентарь", "📊 Статус" },
                new KeyboardButton[] { "💪 Навыки", "🔍 Осмотреть", "⚙️ Помощь" }
            })
            {
                ResizeKeyboard = true
            };
        }

        public static ReplyKeyboardMarkup GetMovementKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "⬆️ Север", "⬇️ Юг" },
                new KeyboardButton[] { "⬅️ Запад", "➡️ Восток" },
                new KeyboardButton[] { "🗺️ Карта мира", "🎒 Инвентарь", "📊 Статус" },
                new KeyboardButton[] { "🔍 Осмотреть", "💬 Поговорить", "⚔️ Атаковать" }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}