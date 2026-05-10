using System.Collections.Generic;
using System.Threading.Tasks;


namespace TelegramCasinoBot.Services.Models.Data.Creation
{
    public interface IRaceService
    {
        Task<IReadOnlyList<Race>> GetAllRacesAsync();
        Task<Race> GetRaceByIdAsync(int id);
        Task<bool> RaceExistsAsync(int id);
        Task<Race> GetRaceByNameAsync(string name);
    }
}