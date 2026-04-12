using System.Collections.Generic;
using System.Threading.Tasks;


namespace TelegramCasinoBot.Services.Data
{
    public interface IRaceService
    {
        Task<IReadOnlyList<Race>> GetAllRacesAsync();
        Task<Race> GetRaceByIdAsync(int id);
        Task<bool> RaceExistsAsync(int id);
        Task<Race> GetRaceByNameAsync(string name);
    }
}