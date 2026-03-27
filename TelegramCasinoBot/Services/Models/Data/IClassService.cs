using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Stats;

namespace TelegramCasinoBot.Services.Data
{
    public interface IClassService
    {
        Task<IReadOnlyList<Class>> GetAllClassesAsync();
        Task<Class> GetClassByIdAsync(int id);
        Task<bool> ClassExistsAsync(int id);
    }
}