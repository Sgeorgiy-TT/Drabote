using System.Collections.Generic;
using TelegramCasinoBot.Models.Stats;

namespace TelegramCasinoBot.Services.Models.Data
{
    public interface IRaceService
    {
        IReadOnlyList<Race> GetAllRaces();
        Race GetRaceById(string id);
        bool RaceExists(string id);
    }
}