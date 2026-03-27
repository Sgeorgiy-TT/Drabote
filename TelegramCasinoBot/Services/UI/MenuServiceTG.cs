using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Models.Gameplay;

namespace TelegramCasinoBot.Services.UI
{
    public class MenuServiceTG
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseService _databaseService;
        private readonly MusicService _musicService;
        private readonly CharacterCreationService _characterCreationService;
        private readonly ImageService _imageService;
        private readonly ILogger<MenuServiceTG> _logger;
        private readonly Dictionary<long, bool> _musicStarted = new();

        public MenuServiceTG(TelegramBotClient botClient, DatabaseService databaseService,
                             MusicService musicService, CharacterCreationService characterCreationService,
                             ImageService imageService,
                             ILogger<MenuServiceTG> logger)
        {
            _botClient = botClient;
            _databaseService = databaseService;
            _musicService = musicService;
            _characterCreationService = characterCreationService;
            _imageService = imageService;
            _logger = logger;
        }

        public async Task ShowMainMenu(long chatId)
        {
            _logger.LogDebug("Начало ShowMainMenu для chatId {ChatId}", chatId);
            try
            {
                //if (!_musicStarted.ContainsKey(chatId) || !_musicStarted[chatId])
                //{
                //    await _musicService.StartBackgroundMusic(chatId);
                //    _musicStarted[chatId] = true;
                //}

                var hasSave = await _databaseService.GetPlayerSaveAsync(chatId) != null;

                var menuText = @"🎮 *METROIDVANIA BOT* 🎮

Добро пожаловать в мир Аркадии! 
Исследуйте древние руины, находите артефакты 
и раскройте тайны забытой цивилизации.";

                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "🎮 Продолжить", "🚀 Новая игра" },
                    new KeyboardButton[] { "💾 Загрузить", "⚙️ Настройки" },
                    new KeyboardButton[] { "🎵 Стоп музыка", "❌ Выход" }
                })
                {
                    ResizeKeyboard = true
                };

                if (!hasSave)
                {
                    keyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "🚀 Новая игра" },
                        new KeyboardButton[] { "💾 Загрузить", "⚙️ Настройки" },
                        new KeyboardButton[] { "🎵 Стоп музыка", "❌ Выход" }
                    })
                    {
                        ResizeKeyboard = true
                    };
                }

                try
                {
                    var imagePath = "maxresdefault.jpg";
                    using var stream = await _imageService.GetProcessedImageAsync(imagePath, ImageService.MenuCategory);
                    await _botClient.SendPhotoAsync(chatId, new InputOnlineFile(stream, "main_menu.jpg"), caption: menuText, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
                }
                catch (FileNotFoundException)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: menuText,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "🖼️ *Совет:* Добавьте изображение в папку Assets/maxresdefault.jpg для красивого меню!",
                        parseMode: ParseMode.Markdown);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка загрузки изображения: {Message}", ex.Message);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: menuText,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard);
                }
            }
            finally
            {
                _logger.LogDebug("ShowMainMenu завершён для chatId {ChatId}", chatId);
            }
        }
        public async Task HandleMenuCommand(long chatId, string command)
        {
            _logger.LogDebug("Начало HandleMenuCommand для chatId {ChatId}, command {Command}", chatId, command);
            try
            {
                if (_characterCreationService.IsInCharacterCreation(chatId))
                {
                    await HandleCharacterCreationInput(chatId, command);
                    return;
                }

                switch (command.ToLower())
                {
                    case "🎮 продолжить":
                    case "продолжить":
                        await ContinueGame(chatId);
                        break;
                    case "🚀 новая игра":
                    case "новая игра":
                        await StartNewGame(chatId);
                        break;
                    case "💾 загрузить":
                    case "загрузить":
                        await ShowLoadMenu(chatId);
                        break;
                    case "⚙️ настройки":
                    case "настройки":
                        await ShowSettings(chatId);
                        break;
                    case "🎵 стоп музыка":
                    case "стоп музыка":
                        await StopMusic(chatId);
                        break;
                    case "🎵 старт музыка":
                    case "старт музыка":
                        await StartMusic(chatId);
                        break;
                    case "❌ выход":
                    case "выход":
                        await ExitGame(chatId);
                        break;
                    default:
                        await ShowMainMenu(chatId);
                        break;
                }
            }
            finally
            {
                _logger.LogDebug("HandleMenuCommand завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task HandleCharacterCreationInput(long chatId, string command)
        {
            var playerInProgress = _characterCreationService.GetCharacterInProgress(chatId);

            if (playerInProgress != null)
            {
                if (string.IsNullOrEmpty(playerInProgress.Name))
                {
                    await _characterCreationService.HandleNameInput(chatId, command);
                }
                else if (string.IsNullOrEmpty(playerInProgress.Gender))
                {
                    await _characterCreationService.HandleGenderInput(chatId, command);
                }
            }
        }

        private async Task StartMusic(long chatId)
        {
            _logger.LogDebug("Начало StartMusic для chatId {ChatId}", chatId);
            try
            {
                await _musicService.StartBackgroundMusic(chatId);
                _musicStarted[chatId] = true;
                await _botClient.SendTextMessageAsync(chatId, "🎵 Фоновая музыка запущена!");
            }
            finally
            {
                _logger.LogDebug("StartMusic завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task StopMusic(long chatId)
        {
            _logger.LogDebug("Начало StopMusic для chatId {ChatId}", chatId);
            try
            {
                await _musicService.StopBackgroundMusic(chatId);
                _musicStarted[chatId] = false;
                await _botClient.SendTextMessageAsync(chatId, "🔇 Фоновая музыка остановлена!");
            }
            finally
            {
                _logger.LogDebug("StopMusic завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task ContinueGame(long chatId)
        {
            _logger.LogDebug("Начало ContinueGame для chatId {ChatId}", chatId);
            try
            {
                var save = await _databaseService.GetPlayerSaveAsync(chatId);
                if (save != null)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "🔄 Загружаем ваше последнее сохранение...");
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Сохранение не найдено. Начните новую игру!",
                        replyMarkup: GetMainMenuKeyboard());
                }
            }
            finally
            {
                _logger.LogDebug("ContinueGame завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task StartNewGame(long chatId)
        {
            _logger.LogDebug("Начало StartNewGame для chatId {ChatId}", chatId);
            try
            {
                await _characterCreationService.StartCharacterCreation(chatId);
            }
            finally
            {
                _logger.LogDebug("StartNewGame завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task ShowLoadMenu(long chatId)
        {
            _logger.LogDebug("Начало ShowLoadMenu для chatId {ChatId}", chatId);
            try
            {
                var saves = await _databaseService.GetPlayerSavesAsync(chatId);

                if (saves.Count > 0)
                {
                    var loadText = "💾 *СОХРАНЕНИЯ*\n\n";
                    var keyboardButtons = new List<InlineKeyboardButton[]>();

                    foreach (var save in saves)
                    {
                        loadText += $"🕐 {save.LastPlayed:dd.MM.yyyy HH:mm}\n";
                        loadText += $"📍 {save.CurrentLocation} | ⭐ Ур. {save.Level}\n";
                        loadText += $"❤️ {save.Health}/{save.MaxHealth} | 🕒 {save.PlayTimeMinutes} мин.\n\n";

                        keyboardButtons.Add(new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                $"🕐 {save.LastPlayed:HH:mm} - Ур. {save.Level}",
                                $"load_{save.ChatId}_{save.LastPlayed.Ticks}")
                        });
                    }

                    var keyboard = new InlineKeyboardMarkup(keyboardButtons);

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: loadText,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: keyboard);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "💾 Сохранения не найдены. Начните новую игру!",
                        replyMarkup: GetMainMenuKeyboard());
                }
            }
            finally
            {
                _logger.LogDebug("ShowLoadMenu завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task ShowSettings(long chatId)
        {
            _logger.LogDebug("Начало ShowSettings для chatId {ChatId}", chatId);
            try
            {
                var settingsText = @"⚙️ *НАСТРОЙКИ*

🔊 Громкость музыки: ████□□
🔊 Громкость эффектов: █████
🎮 Сложность: Средняя
💬 Уведомления: Включены

Используйте кнопки ниже для изменения настроек:";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔊 Музыка", "settings_music"),
                        InlineKeyboardButton.WithCallbackData("🎮 Сложность", "settings_difficulty")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("💬 Уведомления", "settings_notifications"),
                        InlineKeyboardButton.WithCallbackData("🔙 Назад", "menu_back")
                    }
                });

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: settingsText,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard);
            }
            finally
            {
                _logger.LogDebug("ShowSettings завершён для chatId {ChatId}", chatId);
            }
        }

        private async Task ExitGame(long chatId)
        {
            _logger.LogDebug("Начало ExitGame для chatId {ChatId}", chatId);
            try
            {
                await _musicService.StopBackgroundMusic(chatId);
                if (_musicStarted.ContainsKey(chatId))
                {
                    _musicStarted[chatId] = false;
                }

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "👋 Спасибо за игру! Возвращайтесь скорее!\n\nЧтобы снова открыть меню, отправьте /start",
                    replyMarkup: new ReplyKeyboardRemove());
            }
            finally
            {
                _logger.LogDebug("ExitGame завершён для chatId {ChatId}", chatId);
            }
        }

        private ReplyKeyboardMarkup GetMainMenuKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "🎮 Продолжить", "🚀 Новая игра" },
                new KeyboardButton[] { "💾 Загрузить", "⚙️ Настройки" },
                new KeyboardButton[] { "🎵 Стоп музыка", "❌ Выход" }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}