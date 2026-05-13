using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.UI
{
    public class CharacterIconService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<CharacterIconService> _logger;
        private readonly string _iconsBasePath;
        private readonly string _assetsFullPath;
        private readonly string _baseImagePath;
        private readonly ImageService _imageService;
        private readonly Dictionary<long, CharacterIconSelection> _iconSelections = new();

        public CharacterIconService(TelegramBotClient botClient, IOptions<ImageSettings> imageSettings, ImageService imageService, ILogger<CharacterIconService> logger)
        {
            _botClient = botClient;
            _imageService = imageService;
            _logger = logger;
            _baseImagePath = imageSettings.Value.BaseImagePath;
            _assetsFullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), _baseImagePath));
            _iconsBasePath = Path.Combine(_assetsFullPath, "CharacterIcons");
        }

        private class CharacterIconSelection
        {
            public string Gender { get; set; }
            public string Race { get; set; }
            public List<string> AvailableIcons { get; set; } = new();
            public int CurrentPage { get; set; } = 0;
            public const int IconsPerPage = 6;
            public Func<long, Task> BackCallback { get; set; }
        }

        public async Task StartIconSelection(long chatId, string gender, string race, Func<long, Task> backCallback = null)
        {
            _logger.LogDebug("StartIconSelection: chatId={ChatId}, gender={Gender}, race={Race}", chatId, gender, race);

            if (string.IsNullOrEmpty(gender))
            {
                _logger.LogError("Gender is null or empty for chatId {ChatId}", chatId);
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка: пол не указан. Начните создание заново.");
                return;
            }
            if (string.IsNullOrEmpty(race))
            {
                _logger.LogError("Race is null or empty for chatId {ChatId}", chatId);
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка: раса не указана. Начните создание заново.");
                return;
            }

            try
            {
                var selection = new CharacterIconSelection
                {
                    Gender = gender.ToLower(),
                    Race = race.ToLower(),
                    BackCallback = backCallback
                };

                _logger.LogDebug("Getting available icons for gender={Gender}, race={Race}", gender, race);
                selection.AvailableIcons = await GetAvailableIcons(gender, race);
                _logger.LogDebug("Found {Count} icons", selection.AvailableIcons.Count);

                _iconSelections[chatId] = selection;

                await ShowIconPage(chatId, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StartIconSelection for chatId {ChatId}", chatId);
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка загрузки иконок. Попробуйте ещё раз.");
            }
        }

        private async Task<List<string>> GetAvailableIcons(string gender, string race)
        {
            _logger.LogDebug("GetAvailableIcons: gender={Gender}, race={Race}", gender, race);
            var icons = new List<string>();

            var raceFolderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
            var raceKey = race.ToLower();

            if (!raceFolderMap.TryGetValue(raceKey, out var raceFolder))
            {
                _logger.LogWarning("Неизвестная раса: {Race}, используем 'human'", race);
                raceFolder = "human";
            }

            var racePath = Path.Combine(_iconsBasePath, raceFolder);
            _logger.LogDebug("Ищем иконки в папке: {Path}", racePath);

            if (Directory.Exists(racePath))
            {
                var allFiles = Directory.GetFiles(racePath, "*.*", SearchOption.TopDirectoryOnly);
                var filtered = allFiles
                    .Where(f => {
                        var fileName = Path.GetFileName(f).ToLower();
                        return fileName.StartsWith(genderPrefix) ||
                               fileName.StartsWith($"{genderPrefix}_") ||
                               fileName.Contains($"_{genderPrefix}_");
                    })
                    .ToList();

                foreach (var fullPath in filtered)
                {
                    var relativePath = Path.GetRelativePath(_assetsFullPath, fullPath);
                    icons.Add(relativePath);
                }
                _logger.LogDebug("Найдено {Count} иконок для расы {Race} (папка {Folder})", icons.Count, race, raceFolder);
            }
            else
            {
                _logger.LogWarning("Папка для расы {Race} не найдена: {Path}. Используем иконки по умолчанию.", race, racePath);
            }

            if (!icons.Any())
            {
                _logger.LogDebug("Используем дефолтные иконки для пола {Gender}", gender);
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
                foreach (var fullPath in files)
                {
                    var relativePath = Path.GetRelativePath(_assetsFullPath, fullPath);
                    defaultIcons.Add(relativePath);
                }
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
                await _botClient.SendTextMessageAsync(chatId, "❌ Не найдено подходящих иконок.");
                return;
            }

            await SendIconPage(chatId, pageIcons, selection.CurrentPage, totalPages);
        }

        private async Task SendIconPage(long chatId, List<string> icons, int currentPage, int totalPages)
        {
            var messageText = $"🎨 *ВЫБОР ВНЕШНОСТИ*\n\nВыберите иконку персонажа:\nСтраница {currentPage + 1}/{totalPages}";

            var keyboardButtons = new List<InlineKeyboardButton[]>();
            var row = new List<InlineKeyboardButton>();

            for (int i = 0; i < icons.Count; i++)
            {
                var callbackData = $"select_icon_{i + currentPage * CharacterIconSelection.IconsPerPage}";
                row.Add(InlineKeyboardButton.WithCallbackData($"🎭 {i + 1}", callbackData));

                if (row.Count >= 3 || i == icons.Count - 1)
                {
                    keyboardButtons.Add(row.ToArray());
                    row = new List<InlineKeyboardButton>();
                }
            }

            var navButtons = new List<InlineKeyboardButton>();

            if (currentPage > 0)
                navButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️ Назад", "icons_prev"));

            navButtons.Add(InlineKeyboardButton.WithCallbackData("🔍 Просмотреть все", "preview_all"));

            if (currentPage < totalPages - 1)
                navButtons.Add(InlineKeyboardButton.WithCallbackData("Вперед ➡️", "icons_next"));

            navButtons.Add(InlineKeyboardButton.WithCallbackData("🔙 Назад к выбору", "icon_back"));

            if (navButtons.Any())
                keyboardButtons.Add(navButtons.ToArray());

            var keyboard = new InlineKeyboardMarkup(keyboardButtons);

            await _botClient.SendTextMessageAsync(chatId, messageText, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        public async Task HandleIconSelection(long chatId, string callbackData)
        {
            _logger.LogDebug("HandleIconSelection: chatId={ChatId}, data={Data}", chatId, callbackData);
            try
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
                    case "icon_back":
                        if (selection.BackCallback != null)
                            await selection.BackCallback(chatId);
                        break;
                    default:
                        if (callbackData.StartsWith("select_icon_"))
                        {
                            await ProcessIconSelection(chatId, callbackData);
                        }
                        break;
                }
            }
            finally
            {
                _logger.LogDebug("HandleIconSelection завершён для chatId {ChatId}", chatId);
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

        private async Task SendSelectedIconPreview(long chatId, string iconRelativePath)
        {
            try
            {
                using var stream = await _imageService.GetProcessedImageAsync(iconRelativePath, ImageService.CharacterIconCategory);
                await _botClient.SendPhotoAsync(
                    chatId,
                    new InputOnlineFile(stream, "selected_icon.jpg"),
                    caption: "✅ Иконка выбрана! Подтвердите выбор:",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("✅ Подтвердить", "confirm_icon"),
                            InlineKeyboardButton.WithCallbackData("🔄 Выбрать другую", "change_icon")
                        }
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки иконки");
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка загрузки иконки.");
            }
        }

        private async Task PreviewAllIcons(long chatId)
        {
            if (!_iconSelections.ContainsKey(chatId)) return;

            var selection = _iconSelections[chatId];
            var message = $"🎭 *ДОСТУПНЫЕ ИКОНКИ* ({selection.AvailableIcons.Count} шт.):\n\n";

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
            _logger.LogDebug("GetSelectedIconPath для chatId {ChatId}", chatId);
            try
            {
                if (_iconSelections.ContainsKey(chatId) && _iconSelections[chatId].AvailableIcons.Any())
                {
                    return _iconSelections[chatId].AvailableIcons.First();
                }
                return null;
            }
            finally
            {
                _logger.LogDebug("GetSelectedIconPath завершён для chatId {ChatId}", chatId);
            }
        }

        public void ClearSelection(long chatId)
        {
            _logger.LogDebug("ClearSelection для chatId {ChatId}", chatId);
            try
            {
                if (_iconSelections.ContainsKey(chatId))
                {
                    _iconSelections.Remove(chatId);
                }
            }
            finally
            {
                _logger.LogDebug("ClearSelection завершён для chatId {ChatId}", chatId);
            }
        }
    }
}