using System.Collections.Generic;
using TelegramCasinoBot.Models.Stats;

namespace TelegramMetroidvaniaBot.Services.Data
{
    public interface IRaceService
    {
        IReadOnlyList<Race> GetAllRaces();
        Race GetRaceById(string id);
        bool RaceExists(string id);
    }
}