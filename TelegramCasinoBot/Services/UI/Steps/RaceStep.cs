using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Data;
using static Player;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public const string RACE = "race_";
    public class RaceStep : CreationStepBase
    {
        private readonly IRaceService _raceService;
        private PlayerBuilder playerBuilder;

        public RaceStep(TelegramBotClient botClient, IRaceService raceService, Func<long, Task> nextStepCallback, Func<long, Task> restartCallback, RACE)
            : base(botClient, nextStepCallback, restartCallback)
        {
            _raceService = raceService;
        }

        public override async Task Ask(long chatId, PlayerBuilder builder)
        {
            playerBuilder = builder;
            var races = await _raceService.GetAllRacesAsync();
            var buttons = races.Select(race => new[] { InlineKeyboardButton.WithCallbackData(race.Name, CreationResponseIdString(race.Id)) }).ToList();
            var keyboard = new InlineKeyboardMarkup(buttons);
            await _botClient.SendTextMessageAsync(chatId,
                "🎯 *ВЫБОР РАСЫ*\n\nВыберите расу вашего персонажа:",
                parseMode: ParseMode.Markdown,
                replyMarkup: keyboard);
        }

        public override async Task Handle(long chatId, string data)
        {
            if (!data.StartsWith("race_")) return;
            if (!int.TryParse(data.Substring(5), out int raceId)) return;

            var race = await _raceService.GetRaceByIdAsync(raceId);
            if (race == null)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Ошибка выбора расы.");
                return;
            }

            playerBuilder.SetRace(race);
            await _nextStepCallback(chatId);
        }

        public override bool CanHandle(string data) => base.CanHandle(data);
    }
}