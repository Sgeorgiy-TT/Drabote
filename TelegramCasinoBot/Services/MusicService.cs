using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
namespace TelegramMetroidvaniaBot.Services
{
    public class MusicService
    {
        private readonly TelegramBotClient _botClient;
        private readonly string _musicFilePath;
        private readonly Dictionary<long, int> _musicMessageIds = new Dictionary<long, int>();
        private readonly Dictionary<long, bool> _musicPinned = new Dictionary<long, bool>();
        private readonly ILogger<MusicService> _logger;
        public MusicService(TelegramBotClient botClient, ILogger<MusicService> logger)
        {
            _botClient = botClient;
            _logger = logger;
            _musicFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Pr1.mp3");
        }
        public async Task StartBackgroundMusic(long chatId)
        {
            try
            {
                if (!System.IO.File.Exists(_musicFilePath))
                {
                    await SendMusicNotFoundMessage(chatId);
                    _logger.LogWarning("Музыкальный файл не найден: {FilePath}", _musicFilePath);
                    return;
                }
                if (_musicPinned.ContainsKey(chatId) && _musicPinned[chatId])
                {
                    return;
                }
                using var stream = System.IO.File.OpenRead(_musicFilePath);
                var message = await _botClient.SendAudioAsync(
                    chatId: chatId,
                    audio: new InputOnlineFile(stream, "background_music.mp3"),
                    caption: "?? Фоновая музыка Аркадии",
                    title: "Theme of Arcadia",
                    performer: "Metroidvania Bot OST");
                try
                {
                    await _botClient.PinChatMessageAsync(chatId, message.MessageId);
                    _musicPinned[chatId] = true;
                    _logger.LogDebug("Музыка закреплена для chatId: {ChatId}", chatId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось закрепить музыку: {Message}", ex.Message);
                }
                _musicMessageIds[chatId] = message.MessageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка воспроизведения музыки: {Message}", ex.Message);
            }
        }
        public async Task StopBackgroundMusic(long chatId)
        {
            if (_musicMessageIds.ContainsKey(chatId))
            {
                try
                {
                    if (_musicPinned.ContainsKey(chatId) && _musicPinned[chatId])
                    {
                        await _botClient.UnpinChatMessageAsync(chatId);
                        _musicPinned[chatId] = false;
                    }
                    await _botClient.DeleteMessageAsync(chatId, _musicMessageIds[chatId]);
                    _musicMessageIds.Remove(chatId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить сообщение с музыкой: {Message}", ex.Message);
                }
            }
        }
        public async Task SendMusicNotFoundMessage(long chatId)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "?? *Музыкальное сопровождение*\n\nЧтобы добавить фоновую музыку, поместите файл Pr1.mp3 в папку Assets/",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }
        public bool IsMusicPlaying(long chatId)
        {
            return _musicMessageIds.ContainsKey(chatId);
        }
        public bool IsMusicPinned(long chatId)
        {
            return _musicPinned.ContainsKey(chatId) && _musicPinned[chatId];
        }
    }
}