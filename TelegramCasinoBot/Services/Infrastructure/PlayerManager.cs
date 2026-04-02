using System;
using System.Collections.Generic;
using TelegramCasinoBot.Models;
using TelegramCasinoBot.Models.Character;

namespace TelegramCasinoBot.Services.Infrastructure
{
    public class PlayerManager
    {
        private readonly Dictionary<long, Player> _players = new();

        public Player GetPlayer(long chatId) => _players.TryGetValue(chatId, out var player) ? player : null;
        public void AddOrUpdatePlayer(Player player) => _players[player.ChatId] = player;
        public bool ContainsPlayer(long chatId) => _players.ContainsKey(chatId);
        public void RemovePlayer(long chatId) => _players.Remove(chatId);
        public IEnumerable<Player> GetAllPlayers() => _players.Values;

    }
}