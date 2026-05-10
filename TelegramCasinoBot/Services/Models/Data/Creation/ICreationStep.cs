using System.Threading.Tasks;
using static Player;

namespace TelegramCasinoBot.Services.Models.Data.Creation
{
    public interface ICreationStep
    {
        string CallbackKey { get; }
        Task Ask(long chatId, PlayerBuilder builder);
        Task Handle(long chatId, string data); 
        bool CanHandle(string data);
    }
}