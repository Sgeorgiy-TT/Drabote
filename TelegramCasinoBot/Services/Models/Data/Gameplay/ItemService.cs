using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelegramCasinoBot.Models.Gameplay;

namespace TelegramCasinoBot.Services.Data
{
    public class ItemService
    {
        private readonly ILogger<ItemService> _logger;
        private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "Items.json");
        private List<Item> _weapons;
        private List<Item> _armors;
        private List<Item> _consumables;

        public ItemService(ILogger<ItemService> logger)
        {
            _logger = logger;
            LoadItems();
        }

        private void LoadItems()
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<ItemsContainer>(json);
                _weapons = data?.Weapons ?? new List<Item>();
                _armors = data?.Armors ?? new List<Item>();
                _consumables = data?.Consumables ?? new List<Item>();
                _logger.LogInformation("Загружено оружия: {0}, брони: {1}, расходников: {2}", _weapons.Count, _armors.Count, _consumables.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки предметов");
                _weapons = new List<Item>();
                _armors = new List<Item>();
                _consumables = new List<Item>();
            }
        }

        public List<Item> GetWeapons() => _weapons;
        public List<Item> GetArmors() => _armors;
        public List<Item> GetConsumables() => _consumables;

        public List<Item> GetWeaponsByLevel(int level, string rarity = null)
        {
            var query = _weapons.FindAll(w => w.Level == level);
            if (!string.IsNullOrEmpty(rarity))
                query = query.FindAll(w => w.Rarity == rarity);
            return query;
        }

        public Item GetItemByName(string name)
        {
            return _weapons.FirstOrDefault(i => i.Name == name) ??
                   _armors.FirstOrDefault(i => i.Name == name) ??
                   _consumables.FirstOrDefault(i => i.Name == name);
        }

        public async Task<Item> GetItemByNameAsync(string name)
        {
            return await Task.Run(() => GetItemByName(name));
        }

        public async Task<Item> GetItemByIdAsync(int id)
        {
            var all = _weapons.Concat(_armors).Concat(_consumables);
            return await Task.Run(() => all.FirstOrDefault(i => i.Id == id));
        }
    }

    internal class ItemsContainer
    {
        public List<Item> Weapons { get; set; }
        public List<Item> Armors { get; set; }
        public List<Item> Consumables { get; set; }
    }
}