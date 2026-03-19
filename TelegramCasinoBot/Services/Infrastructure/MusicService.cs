using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace TelegramCasinoBot.Services.Infrastructure
{
    public class MusicService
    {
        private readonly TelegramBotClient _botClient;
        private readonly string _musicFilePath;
        private readonly Dictionary<long, int> _musicMessageIds = new Dictionary<long, int>();
        private readonly Dictionary<long, bool> _musicPinned = new Dictionary<long, bool>();
        private readonly ILogger<MusicService> _logger;

        private const int MaxRetries = 3;
        private const int RetryDelayMs = 2000;
        private const int SendTimeoutSeconds = 30;

        public MusicService(TelegramBotClient botClient, ILogger<MusicService> logger)
        {
            _botClient = botClient;
            _logger = logger;
            _musicFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Pr1.mp3");
        }

        public async Task StartBackgroundMusic(long chatId)
        {
            _logger.LogDebug("Начало StartBackgroundMusic для chatId {ChatId}", chatId);
            try
            {
                try
                {
                    var chat = await _botClient.GetChatAsync(chatId);
                    if (chat.PinnedMessage?.Audio != null &&
                        chat.PinnedMessage.Audio.Title == "Theme of Arcadia")
                    {
                        _logger.LogDebug("Музыка уже закреплена в чате {ChatId}, используем существующую", chatId);
                        _musicMessageIds[chatId] = chat.PinnedMessage.MessageId;
                        _musicPinned[chatId] = true;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось проверить наличие закреплённой музыки для chatId {ChatId}", chatId);
                }

                if (!System.IO.File.Exists(_musicFilePath))
                {
                    await SendMusicNotFoundMessage(chatId);
                    _logger.LogWarning("Музыкальный файл не найден: {FilePath}", _musicFilePath);
                    return;
                }

                if (_musicPinned.ContainsKey(chatId) && _musicPinned[chatId])
                    return;

                if (_musicMessageIds.ContainsKey(chatId))
                    return;

                for (int attempt = 1; attempt <= MaxRetries; attempt++)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(SendTimeoutSeconds));
                        using var stream = System.IO.File.OpenRead(_musicFilePath);

                        var message = await _botClient.SendAudioAsync(
                            chatId: chatId,
                            audio: new InputOnlineFile(stream, "background_music.mp3"),
                            caption: "🎵 Фоновая музыка Аркадии",
                            title: "Theme of Arcadia",
                            performer: "Metroidvania Bot OST",
                            cancellationToken: cts.Token);

                        try
                        {
                            await _botClient.PinChatMessageAsync(chatId, message.MessageId, cancellationToken: cts.Token);
                            _musicPinned[chatId] = true;
                            _logger.LogDebug("Музыка закреплена для chatId: {ChatId}", chatId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Не удалось закрепить музыку для chatId {ChatId}: {Message}", chatId, ex.Message);
                        }

                        _musicMessageIds[chatId] = message.MessageId;
                        _logger.LogInformation("Музыка успешно отправлена для chatId {ChatId}", chatId);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Попытка {Attempt} отправки музыки для chatId {ChatId} отменена по таймауту", attempt, chatId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Попытка {Attempt} отправки музыки для chatId {ChatId} не удалась", attempt, chatId);
                    }

                    if (attempt < MaxRetries)
                        await Task.Delay(RetryDelayMs);
                }

                await _botClient.SendTextMessageAsync(chatId,
                    "❌ Не удалось загрузить фоновую музыку из-за проблем с сетью. Попробуйте позже.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в StartBackgroundMusic для chatId {ChatId}: {Message}", chatId, ex.Message);
            }
            finally
            {
                _logger.LogDebug("StartBackgroundMusic завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task StopBackgroundMusic(long chatId)
        {
            _logger.LogDebug("Начало StopBackgroundMusic для chatId {ChatId}", chatId);
            try
            {
                if (_musicMessageIds.TryGetValue(chatId, out int messageId))
                {
                    try
                    {
                        if (_musicPinned.TryGetValue(chatId, out bool pinned) && pinned)
                        {
                            await _botClient.UnpinChatMessageAsync(chatId);
                            _musicPinned[chatId] = false;
                        }

                        await _botClient.DeleteMessageAsync(chatId, messageId);
                        _musicMessageIds.Remove(chatId);
                        _logger.LogDebug("Музыка остановлена для chatId {ChatId}", chatId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось удалить сообщение с музыкой для chatId {ChatId}: {Message}", chatId, ex.Message);
                    }
                }
            }
            finally
            {
                _logger.LogDebug("StopBackgroundMusic завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task SendMusicNotFoundMessage(long chatId)
        {
            _logger.LogDebug("Начало SendMusicNotFoundMessage для chatId {ChatId}", chatId);
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "🎵 *Музыкальное сопровождение*\n\nЧтобы добавить фоновую музыку, поместите файл Pr1.mp3 в папку Assets/",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            finally
            {
                _logger.LogDebug("SendMusicNotFoundMessage завершён для chatId {ChatId}", chatId);
            }
        }

        public bool IsMusicPlaying(long chatId)
        {
            _logger.LogDebug("IsMusicPlaying для chatId {ChatId}", chatId);
            return _musicMessageIds.ContainsKey(chatId);
        }

        public bool IsMusicPinned(long chatId)
        {
            _logger.LogDebug("IsMusicPinned для chatId {ChatId}", chatId);
            return _musicPinned.TryGetValue(chatId, out bool pinned) && pinned;
        }
    }
}