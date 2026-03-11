using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMetroidvaniaBot.Services
{
    /// <summary>
    /// Вспомогательный класс для создания клавиатур. Устраняет дублирование в сервисах.
    /// </summary>
    public static class KeyboardHelper
    {
        /// <summary>
        /// Расширенная клавиатура управления (движение + инвентарь/навыки/осмотр)
        /// </summary>
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

        /// <summary>
        /// Полная клавиатура движения с действиями (карта мира, поговорить, атаковать)
        /// </summary>
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
