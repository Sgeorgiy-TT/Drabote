using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Data;

namespace TelegramCasinoBot.Services.Gameplay
{
    public class MobSpawnService
    {
        private readonly MobService _mobService;
        private readonly ILogger<MobSpawnService> _logger;
        private readonly Random _rng = new();

        public MobSpawnService(MobService mobService, ILogger<MobSpawnService> logger)
        {
            _mobService = mobService;
            _logger = logger;
        }

        public async Task<List<MobInstance>> GenerateInitialMobs(GameLocation location, int playerX, int playerY, int maxMobs = 5)
        {
            var freeCells = GetFreeCells(location, playerX, playerY);
            int mobCount = _rng.Next(1, maxMobs + 1);
            mobCount = Math.Min(mobCount, freeCells.Count);
            var shuffled = freeCells.OrderBy(_ => _rng.Next()).Take(mobCount).ToList();

            var mobs = new List<MobInstance>();
            foreach (var cell in shuffled)
            {
                var mob = await GetRandomMobForLevel(location.Level ?? 1);
                if (mob != null)
                {
                    mobs.Add(new MobInstance(mob.Id, cell.X, cell.Y, mob.Health, mob.Mana, mob.Stamina));
                }
            }
            _logger.LogDebug("Сгенерировано {Count} мобов для локации {Location}", mobs.Count, location.Name);
            return mobs;
        }

        public async Task RespawnMobsIfNeeded(GameLocation location, List<MobInstance> currentMobs, int playerX, int playerY, int maxMobs = 5)
        {
            if (currentMobs.Count >= maxMobs) return;

            var freeCells = GetFreeCells(location, playerX, playerY);
            var occupied = currentMobs.Select(m => (m.X, m.Y)).ToHashSet();
            freeCells = freeCells.Where(c => !occupied.Contains((c.X, c.Y))).ToList();
            if (freeCells.Count == 0) return;

            int needed = maxMobs - currentMobs.Count;
            int toSpawn = Math.Min(needed, freeCells.Count);
            var chosen = freeCells.OrderBy(_ => _rng.Next()).Take(toSpawn).ToList();

            foreach (var cell in chosen)
            {
                var mob = await GetRandomMobForLevel(location.Level ?? 1);
                if (mob != null)
                {
                    currentMobs.Add(new MobInstance(mob.Id, cell.X, cell.Y, mob.Health, mob.Mana, mob.Stamina));
                }
            }
            _logger.LogDebug("Респавн {Count} мобов в локации {Location}", toSpawn, location.Name);
        }

        private List<Position> GetFreeCells(GameLocation location, int playerX, int playerY)
        {
            var blocked = new HashSet<(int, int)> { (playerX, playerY) };

            void AddObjects(string key)
            {
                if (location.Objects.ContainsKey(key))
                    foreach (var pos in location.Objects[key])
                        blocked.Add((pos.X, pos.Y));
            }

            AddObjects("obstacles");
            AddObjects("chests");
            AddObjects("npcs");
            foreach (var exit in location.Exits)
                blocked.Add((exit.Position.X, exit.Position.Y));

            var cells = new List<Position>();
            for (int x = 0; x < location.Width; x++)
                for (int y = 0; y < location.Height; y++)
                    if (!blocked.Contains((x, y)))
                        cells.Add(new Position(x, y));
            return cells;
        }

        private async Task<Mob> GetRandomMobForLevel(int locationLevel)
        {
            var all = _mobService.GetAllMobs();
            var suitable = all.Where(m => m.Level >= locationLevel - 2 && m.Level <= locationLevel + 2).ToList();
            if (suitable.Count == 0) suitable = all;
            return suitable[_rng.Next(suitable.Count)];
        }
    }
}