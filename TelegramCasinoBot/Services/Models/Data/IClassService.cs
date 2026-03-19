using System.Collections.Generic;
using TelegramCasinoBot.Models.Stats;

namespace TelegramCasinoBot.Services.Models.Data
{
    public interface IClassService
    {
        IReadOnlyList<Class> GetAllClasses();
        Class GetClassById(string id);
        bool ClassExists(string id);
    }
}