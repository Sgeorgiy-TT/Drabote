using System.Threading.Tasks;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public interface ICreationStep
    {
        Task Ask(long chatId);
        Task Handle(long chatId, string data);
        bool CanHandle(string data);
    }
}