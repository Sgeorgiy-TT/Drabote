using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Models.Data.Creation;
using static Player;

namespace TelegramCasinoBot.Services.UI.Steps.StepsCreation
{
    public class RaceStep : CreationStepBase
    {
        public const string RACE = "race_";
        private readonly IRaceService _raceService;
        private readonly ILogger<RaceStep> _logger;

        public RaceStep(TelegramBotClient botClient, IRaceService raceService, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback, Func<long, Task> goBackCallback, ILogger<RaceStep> logger)
            : base(botClient, RACE, nextStepCallback, restartCallback, goBackCallback)
        {
            _logger = logger;
            _raceService = raceService;
        }

        public override async Task Ask(long chatId, PlayerBuilder builder)
        {
            var races = await _raceService.GetAllRacesAsync();
            var buttons = races.Select(race => new[] { InlineKeyboardButton.WithCallbackData(race.Name, CreationResponseIdString(race.Id)) }).ToList();
            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "back") });
            var keyboard = new InlineKeyboardMarkup(buttons);
            await _botClient.SendTextMessageAsync(chatId,
                "🎯 *ВЫБОР РАСЫ*\n\nВыберите расу вашего персонажа:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public override async Task Handle(long chatId, PlayerBuilder builder, string data)
        {
            _logger.LogDebug("[{ChatId}] RaceStep.Handle: data='{Data}'", chatId, data);

            if (data == "back")
            {
                if (_goBackCallback != null)
                    await _goBackCallback(chatId);
                return;
            }

            if (!data.StartsWith("race_")) return;
            if (!int.TryParse(data.Substring(5), out int raceId)) return;
            var race = await _raceService.GetRaceByIdAsync(raceId);
            if (race == null)
            {
                _logger.LogWarning("[{ChatId}] Раса с Id {RaceId} не найдена", chatId, raceId);
                return;
            }
            builder.SetRace(race);
            _logger.LogDebug("[{ChatId}] Установлена раса '{RaceName}'", chatId, race.Name);
            await _nextStepCallback(chatId);
        }

        public override bool CanHandle(string data) => base.CanHandle(data) || data == "back";
    }
}