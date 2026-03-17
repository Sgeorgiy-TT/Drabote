using System.Collections.Generic;
using TelegramMetroidvaniaBot.Models;

namespace TelegramMetroidvaniaBot.Services.Data
{
    public interface IRaceService
    {
        IReadOnlyList<Race> GetAllRaces();
        Race GetRaceById(string id);
        bool RaceExists(string id);
    }
}