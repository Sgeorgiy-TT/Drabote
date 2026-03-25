using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Stats;
using TelegramMetroidvaniaBot.Models;

namespace TelegramCasinoBot.Services.Data
{
    public interface IRaceService
    {
        Task<IReadOnlyList<Race>> GetAllRacesAsync();
        Task<Race> GetRaceByIdAsync(int id);
        Task<bool> RaceExistsAsync(int id);
    }
}