using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using System.Collections.Generic;
using System.Linq;
namespace TelegramMetroidvaniaBot.Services
{
    public class MenuService
    {
        private readonly TelegramBotClient _botClient;
        private readonly DatabaseService _databaseService;
        private readonly MusicService _musicService;
        private readonly CharacterCreationService _characterCreationService;
        private readonly ILogger<MenuService> _logger;
        private readonly Dictionary<long, bool> _musicStarted = new Dictionary<long, bool>();
        public MenuService(TelegramBotClient botClient, DatabaseService databaseService,
                         MusicService musicService, CharacterCreationService characterCreationService,
                         ILogger<MenuService> logger)
        {
            _botClient = botClient;
            _databaseService = databaseService;
            _musicService = musicService;
            _characterCreationService = characterCreationService;
            _logger = logger;
        }
        public async Task ShowMainMenu(long chatId)
        {
            if (!_musicStarted.ContainsKey(chatId) || !_musicStarted[chatId])
            {
                await _musicService.StartBackgroundMusic(chatId);
                _musicStarted[chatId] = true;
            }
            var hasSave = await _databaseService.GetPlayerSaveAsync(chatId) != null;
            var menuText = @"?? *METROIDVANIA BOT* ??
Добро пожаловать в мир Аркадии! 
Исследуйте древние руины, находите артефакты 
и раскройте тайны забытой цивилизации.";
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "?? Продолжить", "?? Новая игра" },
                new KeyboardButton[] { "?? Загрузить", "?? Настройки" },
                new KeyboardButton[] { "?? Стоп музыка", "? Выход" }
            })
            {
                ResizeKeyboard = true
            };
            if (!hasSave)
            {
                keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "?? Новая игра" },
                    new KeyboardButton[] { "?? Загрузить", "?? Настройки" },
                    new KeyboardButton[] { "?? Стоп музыка", "? Выход" }
                })
                {
                    ResizeKeyboard = true
                };
            }
            try
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "maxresdefault.jpg");
                if (System.IO.File.Exists(imagePath))
                {
                    using (var stream = System.IO.File.OpenRead(imagePath))
                    {
                        await _botClient.SendPhotoAsync(
                            chatId: chatId,
                            photo: new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream, "main_menu.jpg"),
                            caption: menuText,
                            parseMode: ParseMode.Markdown,
                            replyMarkup: keyboard);
                    }
                }
                else
                {
                    throw new FileNotFoundException("Изображение не найдено");
                }
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
                    text: "??? *Совет:* Добавьте изображение в папку Assets/maxresdefault.jpg для красивого меню!",
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
        public async Task HandleMenuCommand(long chatId, string command)
        {
            if (_characterCreationService.IsInCharacterCreation(chatId))
            {
                await HandleCharacterCreationInput(chatId, command);
                return;
            }
            switch (command.ToLower())
            {
                case "?? продолжить":
                case "продолжить":
                    await ContinueGame(chatId);
                    break;
                case "?? новая игра":
                case "новая игра":
                    await StartNewGame(chatId);
                    break;
                case "?? загрузить":
                case "загрузить":
                    await ShowLoadMenu(chatId);
                    break;
                case "?? настройки":
                case "настройки":
                    await ShowSettings(chatId);
                    break;
                case "?? стоп музыка":
                case "стоп музыка":
                    await StopMusic(chatId);
                    break;
                case "?? старт музыка":
                case "старт музыка":
                    await StartMusic(chatId);
                    break;
                case "? выход":
                case "выход":
                    await ExitGame(chatId);
                    break;
                default:
                    await ShowMainMenu(chatId);
                    break;
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
            await _musicService.StartBackgroundMusic(chatId);
            _musicStarted[chatId] = true;
            await _botClient.SendTextMessageAsync(chatId, "?? Фоновая музыка запущена!");
        }
        private async Task StopMusic(long chatId)
        {
            await _musicService.StopBackgroundMusic(chatId);
            _musicStarted[chatId] = false;
            await _botClient.SendTextMessageAsync(chatId, "?? Фоновая музыка остановлена!");
        }
        private async Task ContinueGame(long chatId)
        {
            var save = await _databaseService.GetPlayerSaveAsync(chatId);
            if (save != null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "?? Загружаем ваше последнее сохранение...");
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "? Сохранение не найдено. Начните новую игру!",
                    replyMarkup: GetMainMenuKeyboard());
            }
        }
        private async Task StartNewGame(long chatId)
        {
            await _characterCreationService.StartCharacterCreation(chatId);
        }
        private async Task ShowLoadMenu(long chatId)
        {
            var saves = await _databaseService.GetPlayerSavesAsync(chatId);
            if (saves.Count > 0)
            {
                var loadText = "?? *СОХРАНЕНИЯ*\n\n";
                var keyboardButtons = new List<InlineKeyboardButton[]>();
                foreach (var save in saves)
                {
                    loadText += $"?? {save.LastPlayed:dd.MM.yyyy HH:mm}\n";
                    loadText += $"?? {save.CurrentLocation} | ? Ур. {save.Level}\n";
                    loadText += $"?? {save.Health}/{save.MaxHealth} | ?? {save.PlayTimeMinutes} мин.\n\n";
                    keyboardButtons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            $"?? {save.LastPlayed:HH:mm} - Ур. {save.Level}",
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
                    text: "?? Сохранения не найдены. Начните новую игру!",
                    replyMarkup: GetMainMenuKeyboard());
            }
        }
        private async Task ShowSettings(long chatId)
        {
            var settingsText = @"?? *НАСТРОЙКИ*
?? Громкость музыки: ----??
?? Громкость эффектов: -----
?? Сложность: Средняя
?? Уведомления: Включены
Используйте кнопки ниже для изменения настроек:";
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("?? Музыка", "settings_music"),
                    InlineKeyboardButton.WithCallbackData("?? Сложность", "settings_difficulty")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("?? Уведомления", "settings_notifications"),
                    InlineKeyboardButton.WithCallbackData("?? Назад", "menu_back")
                }
            });
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: settingsText,
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }
        private async Task ExitGame(long chatId)
        {
            await _musicService.StopBackgroundMusic(chatId);
            if (_musicStarted.ContainsKey(chatId))
            {
                _musicStarted[chatId] = false;
            }
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "?? Спасибо за игру! Возвращайтесь скорее!\n\nЧтобы снова открыть меню, отправьте /start",
                replyMarkup: new ReplyKeyboardRemove());
        }
        private ReplyKeyboardMarkup GetMainMenuKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "?? Продолжить", "?? Новая игра" },
                new KeyboardButton[] { "?? Загрузить", "?? Настройки" },
                new KeyboardButton[] { "?? Стоп музыка", "? Выход" }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}