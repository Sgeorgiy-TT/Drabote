using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Data.Gameplay;

namespace TelegramCasinoBot.Services.Data
{
    public class MobService
    {
        private readonly ILogger<MobService> _logger;
        private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "Mobs.json");
        private List<Mob> _mobs;
        private readonly ItemService _itemService;
        public MobService(ILogger<MobService> logger, ItemService itemService)
        {
            _logger = logger;
            LoadMobs();
            _itemService = itemService;
        }

        private void LoadMobs()
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<MobsContainer>(json);
                _mobs = data?.Mobs ?? new List<Mob>();
                _logger.LogInformation("Загружено мобов: {Count}", _mobs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки мобов");
                _mobs = new List<Mob>();
            }
        }

        public Mob GetMobById(int id) => _mobs.FirstOrDefault(m => m.Id == id);
        public List<Mob> GetAllMobs() => _mobs;

        public async Task<Item> GetRandomDropAsync(Mob mob, Random rng = null)
        {
            rng ??= new Random();
            if (mob.DropTable?.Items == null || mob.DropTable.Items.Count == 0)
                return null;

            var totalChance = mob.DropTable.Items.Sum(i => i.Chance);
            var roll = rng.NextDouble() * totalChance;
            double cumulative = 0;
            foreach (var drop in mob.DropTable.Items)
            {
                cumulative += drop.Chance;
                if (roll <= cumulative)
                {
                    return await _itemService.GetItemByIdAsync(drop.ItemId);
                }
            }
            return null;
        }
    }

    internal class MobsContainer
    {
        public List<Mob> Mobs { get; set; }
    }
}