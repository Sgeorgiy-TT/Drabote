using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TelegramMetroidvaniaBot.Services
{
    public class MessageThrottlingService
    {
        private readonly ILogger<MessageThrottlingService> _logger;
        private readonly Dictionary<long, DateTime> _lastMessageTimes = new Dictionary<long, DateTime>();
        private readonly TimeSpan _minDelay = TimeSpan.FromMilliseconds(500);

        public MessageThrottlingService(ILogger<MessageThrottlingService> logger = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ThrottleAsync(long chatId)
        {
            _logger.LogDebug("Начало ThrottleAsync для chatId {ChatId}", chatId);
            try
            {
                if (_lastMessageTimes.ContainsKey(chatId))
                {
                    var timeSinceLastMessage = DateTime.Now - _lastMessageTimes[chatId];
                    if (timeSinceLastMessage < _minDelay)
                    {
                        var delayTime = _minDelay - timeSinceLastMessage;
                        _logger.LogDebug("Throttling message for chatId {ChatId}, delay: {Delay}ms", chatId, delayTime.TotalMilliseconds);
                        await Task.Delay(delayTime);
                    }
                }

                _lastMessageTimes[chatId] = DateTime.Now;
            }
            finally
            {
                _logger.LogDebug("ThrottleAsync завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task SendWithThrottle(Func<Task> sendAction, long chatId)
        {
            _logger.LogDebug("Начало SendWithThrottle для chatId {ChatId}", chatId);
            try
            {
                await ThrottleAsync(chatId);
                await sendAction();
            }
            finally
            {
                _logger.LogDebug("SendWithThrottle завершён для chatId {ChatId}", chatId);
            }
        }
    }
}