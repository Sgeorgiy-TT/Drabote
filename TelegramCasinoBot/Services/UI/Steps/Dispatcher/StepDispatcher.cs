using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using TelegramCasinoBot.Services.Models.Data.Creation;
//подумать как все систематизировать
namespace TelegramCasinoBot.Services.UI.Steps.Dispatcher
{
    public class StepDispatcher
    {
        private readonly List<ICreationStep> _steps;
        private readonly TelegramBotClient _botClient;

        public StepDispatcher(List<ICreationStep> steps, TelegramBotClient botClient)
        {
            _steps = steps;
            _botClient = botClient;
        }

        public async Task Dispatch(long chatId, string data)
        {
            var step = _steps.FirstOrDefault(s => s.CanHandle(data));
            if (step != null)
            {
                await step.Handle(chatId, data);
            }
            else
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Пожалуйста, следуйте инструкциям.");
            }
        }
    }
}