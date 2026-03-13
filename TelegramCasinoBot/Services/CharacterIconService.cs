using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
namespace TelegramMetroidvaniaBot.Services
{
    public class CharacterIconService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<CharacterIconService> _logger;
        private readonly string _iconsBasePath;
        private readonly Dictionary<long, CharacterIconSelection> _iconSelections = new Dictionary<long, CharacterIconSelection>();
        public CharacterIconService(TelegramBotClient botClient, ILogger<CharacterIconService> logger)
        {
            _botClient = botClient;
            _logger = logger;
            _iconsBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "CharacterIcons");
        }
        private class CharacterIconSelection
        {
            public string Gender { get; set; }
            public string Race { get; set; }
            public List<string> AvailableIcons { get; set; } = new List<string>();
            public int CurrentPage { get; set; } = 0;
            public const int IconsPerPage = 6;
        }
        public async Task StartIconSelection(long chatId, string gender, string race)
        {
            _logger.LogDebug("Начало выбора иконки для chatId={ChatId}, gender={Gender}, race={Race}", chatId, gender, race);
            var selection = new CharacterIconSelection
            {
                Gender = gender.ToLower(),
                Race = race.ToLower()
            };
            selection.AvailableIcons = await GetAvailableIcons(gender, race);
            _iconSelections[chatId] = selection;
            _logger.LogDebug("Загружено {Count} иконок для chatId={ChatId}", selection.AvailableIcons.Count, chatId);
            await ShowIconPage(chatId, 0);
        }
        private async Task<List<string>> GetAvailableIcons(string gender, string race)
        {
            var icons = new List<string>();
            var raceFolderMap = new Dictionary<string, string>
            {
                ["человек"] = "human",
                ["human"] = "human",
                ["эльф"] = "elves",
                ["elf"] = "elves",
                ["elve"] = "elves",
                ["орк"] = "orc",
                ["orc"] = "orc",
                ["гном"] = "dwarf",
                ["dwarf"] = "dwarf",
                ["драконид"] = "draconian",
                ["dragonkin"] = "draconian",
                ["draconian"] = "draconian"
            };
            var genderPrefix = gender.ToLower() == "male" ? "male" : "female";
            var raceFolder = raceFolderMap.ContainsKey(race.ToLower()) ? raceFolderMap[race.ToLower()] : "human";
            var racePath = Path.Combine(_iconsBasePath, raceFolder);
            if (Directory.Exists(racePath))
            {
                var allFiles = Directory.GetFiles(racePath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => {
                        var fileName = Path.GetFileName(f).ToLower();
                        return fileName.StartsWith(genderPrefix) ||
                               fileName.StartsWith($"{genderPrefix}_") ||
                               fileName.Contains($"_{genderPrefix}_");
                    })
                    .ToList();
                icons.AddRange(allFiles);
            }
            if (!icons.Any())
            {
                icons.AddRange(await GetDefaultIcons(gender));
            }
            return icons.OrderBy(f => f).ToList();
        }
        private async Task<List<string>> GetDefaultIcons(string gender)
        {
            var defaultIcons = new List<string>();
            var genderPrefix = gender.ToLower() == "male" ? "male" : "female";
            foreach (var raceFolder in Directory.GetDirectories(_iconsBasePath))
            {
                var files = Directory.GetFiles(raceFolder, $"{genderPrefix}*.*");
                defaultIcons.AddRange(files);
            }
            return defaultIcons.Take(10).ToList(); 
        }
        private async Task ShowIconPage(long chatId, int page)
        {
            if (!_iconSelections.ContainsKey(chatId)) return;
            var selection = _iconSelections[chatId];
            var totalPages = (int)Math.Ceiling((double)selection.AvailableIcons.Count / CharacterIconSelection.IconsPerPage);
            selection.CurrentPage = Math.Clamp(page, 0, totalPages - 1);
            var pageIcons = selection.AvailableIcons
                .Skip(selection.CurrentPage * CharacterIconSelection.IconsPerPage)
                .Take(CharacterIconSelection.IconsPerPage)
                .ToList();
            if (!pageIcons.Any())
            {
                await _botClient.SendTextMessageAsync(chatId, "? Не найдено подходящих иконок.");
                return;
            }
            await SendIconPage(chatId, pageIcons, selection.CurrentPage, totalPages);
        }
        private async Task SendIconPage(long chatId, List<string> icons, int currentPage, int totalPages)
        {
            var messageText = $"?? *ВЫБОР ВНЕШНОСТИ*\n\nВыберите иконку персонажа:\nСтраница {currentPage + 1}/{totalPages}";
            var keyboardButtons = new List<InlineKeyboardButton[]>();
            var row = new List<InlineKeyboardButton>();
            for (int i = 0; i < icons.Count; i++)
            {
                var iconPath = icons[i];
                var iconName = Path.GetFileNameWithoutExtension(iconPath);
                var callbackData = $"select_icon_{i + (currentPage * CharacterIconSelection.IconsPerPage)}";
                row.Add(InlineKeyboardButton.WithCallbackData($"?? {i + 1}", callbackData));
                if (row.Count >= 3 || i == icons.Count - 1)
                {
                    keyboardButtons.Add(row.ToArray());
                    row = new List<InlineKeyboardButton>();
                }
            }
            var navButtons = new List<InlineKeyboardButton>();
            if (currentPage > 0)
                navButtons.Add(InlineKeyboardButton.WithCallbackData("?? Назад", "icons_prev"));
            navButtons.Add(InlineKeyboardButton.WithCallbackData("?? Просмотреть все", "preview_all"));
            if (currentPage < totalPages - 1)
                navButtons.Add(InlineKeyboardButton.WithCallbackData("Вперед ??", "icons_next"));
            if (navButtons.Any())
                keyboardButtons.Add(navButtons.ToArray());
            var keyboard = new InlineKeyboardMarkup(keyboardButtons);
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: messageText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }
        public async Task HandleIconSelection(long chatId, string callbackData)
        {
            if (!_iconSelections.ContainsKey(chatId)) return;
            var selection = _iconSelections[chatId];
            switch (callbackData)
            {
                case "icons_prev":
                    await ShowIconPage(chatId, selection.CurrentPage - 1);
                    break;
                case "icons_next":
                    await ShowIconPage(chatId, selection.CurrentPage + 1);
                    break;
                case "preview_all":
                    await PreviewAllIcons(chatId);
                    break;
                default:
                    if (callbackData.StartsWith("select_icon_"))
                    {
                        await ProcessIconSelection(chatId, callbackData);
                    }
                    break;
            }
        }
        private async Task ProcessIconSelection(long chatId, string callbackData)
        {
            if (!_iconSelections.ContainsKey(chatId)) return;
            var selection = _iconSelections[chatId];
            var iconIndex = int.Parse(callbackData.Substring("select_icon_".Length));
            if (iconIndex >= 0 && iconIndex < selection.AvailableIcons.Count)
            {
                var selectedIcon = selection.AvailableIcons[iconIndex];
                await SendSelectedIconPreview(chatId, selectedIcon);
            }
        }
        private async Task SendSelectedIconPreview(long chatId, string iconPath)
        {
            try
            {
                using (var stream = System.IO.File.OpenRead(iconPath))
                {
                    await _botClient.SendPhotoAsync(
                        chatId: chatId,
                        photo: new InputOnlineFile(stream, "selected_icon.jpg"),
                        caption: "? Иконка выбрана! Подтвердите выбор:",
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("? Подтвердить", "confirm_icon"),
                                InlineKeyboardButton.WithCallbackData("?? Выбрать другую", "change_icon")
                            }
                        }));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки иконки: {Message}", ex.Message);
                await _botClient.SendTextMessageAsync(chatId, "? Ошибка загрузки иконки. Попробуйте выбрать другую.");
            }
        }
        private async Task PreviewAllIcons(long chatId)
        {
            if (!_iconSelections.ContainsKey(chatId)) return;
            var selection = _iconSelections[chatId];
            var message = $"?? *ДОСТУПНЫЕ ИКОНКИ* ({selection.AvailableIcons.Count} шт.):\n\n";
            for (int i = 0; i < selection.AvailableIcons.Count; i++)
            {
                var iconPath = selection.AvailableIcons[i];
                var fileName = Path.GetFileName(iconPath);
                message += $"{i + 1}. {fileName}\n";
            }
            await _botClient.SendTextMessageAsync(chatId, message, parseMode: ParseMode.Markdown);
        }
        public string GetSelectedIconPath(long chatId)
        {
            if (_iconSelections.ContainsKey(chatId) && _iconSelections[chatId].AvailableIcons.Any())
            {
                return _iconSelections[chatId].AvailableIcons.First();
            }
            return null;
        }
        public void ClearSelection(long chatId)
        {
            if (_iconSelections.ContainsKey(chatId))
            {
                _iconSelections.Remove(chatId);
            }
        }
    }
}