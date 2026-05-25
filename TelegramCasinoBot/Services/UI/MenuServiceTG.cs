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
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.UI
{
    public class MenuServiceTG
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseService _databaseService;
        private readonly MusicService _musicService;
        private readonly ImageService _imageService;
        private readonly ILogger<MenuServiceTG> _logger;
        private readonly PlayerManager _playerManager;
        private readonly Dictionary<long, bool> _musicStarted = new();
        private readonly LocationService _locationService;

        public MenuServiceTG(TelegramBotClient botClient, DatabaseService databaseService,
                             MusicService musicService, ImageService imageService,
                             ILogger<MenuServiceTG> logger, PlayerManager playerManager, LocationService locationService)
        {
            _botClient = botClient;
            _databaseService = databaseService;
            _musicService = musicService;
            _imageService = imageService;
            _logger = logger;
            _playerManager = playerManager;
            _locationService = locationService;
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
                    new KeyboardButton[] { "🎵 Стоп музыка" }
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
                        new KeyboardButton[] { "🎵 Стоп музыка" }
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
                
                switch (command.ToLower())
                {
                    case "🎮 продолжить":
                    case "продолжить":
                        await ContinueGame(chatId);
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
            var player = _playerManager.GetPlayer(chatId);
            if (player == null)
            {
                player = await _databaseService.GetPlayerSaveAsync(chatId);
                if (player != null)
                    _playerManager.AddOrUpdatePlayer(player);
            }
            if (player != null)
            {
                await _botClient.SendTextMessageAsync(chatId, "🔄 Загружаем игру...");
                await _locationService.DescribeLocation(chatId, player);
                await _botClient.SendTextMessageAsync(chatId, "✅ Игра загружена", replyMarkup: KeyboardHelper.GetMovementKeyboard());
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Сохранение не найдено.", replyMarkup: GetMainMenuKeyboard());
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
                        loadText += $"❤️ {save.Health.Current}/{save.Health.Max} | 🕒 {save.PlayTimeMinutes} мин.\n\n";

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

        public async Task ShowSettings(long chatId)
        {
            var player = _playerManager.GetPlayer(chatId);
            var speed = player?.SpeedBoost ?? 1;

            var settingsText = $@"⚙️ *НАСТРОЙКИ*

⚡ Скорость передвижения: {speed} клеток

Используйте кнопку ниже для изменения скорости:";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[] { InlineKeyboardButton.WithCallbackData("⚡ Скорость", "settings_speed") }
    });
            await _botClient.SendTextMessageAsync(chatId, settingsText, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        public async Task ShowSpeedSettings(long chatId)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player == null) return;

            var keyboard = new InlineKeyboardMarkup(new[]
                    {
                new[] { InlineKeyboardButton.WithCallbackData("🐢 1 клетка", "speed_1") },
                new[] { InlineKeyboardButton.WithCallbackData("🚶 2 клетки", "speed_2") },
                new[] { InlineKeyboardButton.WithCallbackData("🏃 3 клетки", "speed_3") },
                new[] { InlineKeyboardButton.WithCallbackData("⚡ 4 клетки", "speed_4") },
                new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "settings_back")}
            });
            await _botClient.SendTextMessageAsync(chatId, "⚡ *Выберите скорость передвижения:*", parseMode: ParseMode.Markdown, replyMarkup: keyboard);
        }

        public async Task SetSpeed(long chatId, int speed)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
            {
                player.SpeedBoost = speed;
                await _databaseService.SavePlayerAsync(player);
                await _botClient.SendTextMessageAsync(chatId, $"✅ Скорость передвижения установлена на {speed} клеток.");
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Игрок не найден.");
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
                new KeyboardButton[] { "🎵 Стоп музыка" }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}