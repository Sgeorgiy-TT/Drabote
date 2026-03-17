using System.Collections.Generic;
using TelegramMetroidvaniaBot.Models;

namespace TelegramMetroidvaniaBot.Services.Data
{
    public interface IClassService
    {
        IReadOnlyList<CharacterClass> GetAllClasses();
        CharacterClass GetClassById(string id);
        bool ClassExists(string id);
    }
}