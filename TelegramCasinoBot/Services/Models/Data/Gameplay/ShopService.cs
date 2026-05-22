using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Data.Gameplay;

namespace TelegramCasinoBot.Services.Data
{
    public class ShopService
    {
        private readonly ILogger<ShopService> _logger;
        private readonly ItemService _itemService;
        private readonly AbilityService _abilityService;

        public ShopService(ILogger<ShopService> logger, ItemService itemService, AbilityService abilityService)
        {
            _logger = logger;
            _itemService = itemService;
            _abilityService = abilityService;
        }

        public List<(Item Item, int Price)> GetAvailableItems(int playerLevel)
        {
            return _itemService.GetAllItems()
                .Where(i => i.IsSoldByTrader && i.Level <= playerLevel)
                .Select(i => (Item: i, Price: i.Price ?? 0))
                .Where(t => t.Price > 0)
                .ToList();
        }

        public List<(Ability Ability, int Price)> GetAvailableAbilities(int playerLevel)
        {
            return _abilityService.GetPlayerAbilities()
                .Where(a => a.IsSoldByTrader && a.MinLevel <= playerLevel)
                .Select(a => (Ability: a, Price: a.Price ?? 0))
                .Where(t => t.Price > 0)
                .ToList();
        }
    }
}