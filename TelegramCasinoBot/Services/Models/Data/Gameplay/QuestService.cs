using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TelegramCasinoBot.Models.Gameplay;

namespace TelegramCasinoBot.Services.Models.Data.Gameplay
{
    public class QuestService
    {
        private readonly ILogger<QuestService> _logger;
        private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Data", "Quests.json");
        private List<Quest> _quests;

        public QuestService(ILogger<QuestService> logger)
        {
            _logger = logger;
            LoadQuests();
        }

        private void LoadQuests()
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                _quests = JsonSerializer.Deserialize<List<Quest>>(json) ?? new List<Quest>();
                _logger.LogInformation("Загружено квестов: {Count}", _quests.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки квестов");
                _quests = new List<Quest>();
            }
        }

        public List<Quest> GetAllQuests() => _quests;
        public Quest GetQuestById(int id) => _quests.FirstOrDefault(q => q.Id == id);
    }
}
