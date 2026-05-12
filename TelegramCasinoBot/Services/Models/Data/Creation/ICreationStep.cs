using System.Threading.Tasks;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public interface ICreationStep
    {
        string CallbackKey { get; }
        Task Ask(long chatId, Player.PlayerBuilder builder);
        Task Handle(long chatId, Player.PlayerBuilder builder, string data);
        bool CanHandle(string data);
    }
}