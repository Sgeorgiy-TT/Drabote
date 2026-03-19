using System.Collections.Generic;
using TelegramCasinoBot.Models.Stats;

namespace TelegramMetroidvaniaBot.Services.Data
{
    public interface IClassService
    {
        IReadOnlyList<Class> GetAllClasses();
        Class GetClassById(string id);
        bool ClassExists(string id);
    }
}