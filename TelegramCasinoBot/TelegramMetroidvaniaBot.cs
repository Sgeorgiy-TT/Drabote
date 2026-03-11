using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TelegramMetroidvaniaBot.Services
{
    public class MessageThrottlingService
    {
        private readonly Dictionary<long, DateTime> _lastMessageTimes = new Dictionary<long, DateTime>();
        private readonly TimeSpan _minDelay = TimeSpan.FromMilliseconds(500); // Минимальная задержка 500ms

        public async Task ThrottleAsync(long chatId)
        {
            if (_lastMessageTimes.ContainsKey(chatId))
            {
                var timeSinceLastMessage = DateTime.Now - _lastMessageTimes[chatId];
                if (timeSinceLastMessage < _minDelay)
                {
                    var delayTime = _minDelay - timeSinceLastMessage;
                    await Task.Delay(delayTime);
                }
            }

            _lastMessageTimes[chatId] = DateTime.Now;
        }

        public async Task SendWithThrottle(Func<Task> sendAction, long chatId)
        {
            await ThrottleAsync(chatId);
            await sendAction();
        }
    }
}