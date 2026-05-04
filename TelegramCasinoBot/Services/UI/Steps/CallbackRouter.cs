using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramCasinoBot.Services.Gameplay;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI.Handlers;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class CallbackRouter
    {
        private readonly List<(string Key, Func<long, string, CallbackQuery, Task> Handler, bool IsPrefix)> _handlers;

        public const string REFRESH_MAP = "refresh_map";
        public const string SHOW_LOCATION = "show_location";
        public const string ATTACK_BOSS = "attack_boss";
        public const string DEFEND_BOSS = "defend_boss";
        public const string ABILITY_BOSS = "ability_boss";
        public const string FLEE_BOSS = "flee_boss";
        public const string TAKE = "take_";
        public const string EXAMINE = "examine_";
        public const string USE = "use_";
        public const string DROP = "drop_";
        public const string MOVE = "move_";
        public const string LEARN_LASER = "learn_laser";
        public const string ATTACK_CRYSTAL = "attack_crystal";

        public const string SELECT_ICON = "select_icon_";
        public const string ICONS_PREV = "icons_prev";
        public const string ICONS_NEXT = "icons_next";
        public const string PREVIEW_ALL = "preview_all";

        public const string CLASS = "class_";
        public const string CONFIRM_ICON = "confirm_icon";
        public const string CONFIRM_CHARACTER = "confirm_character";
        public const string RESTART_CHARACTER = "restart_character";
        public const string NAME = "name";
        public const string GENDER = "gender";
        //сервисы все убрать а список шагов получать в конструкторе
        public CallbackRouter(
            TelegramBotClient botClient,
            GameMenuHandler gameMenuHandler,
            BattleHandler battleHandler,
            InventoryHandler inventoryHandler,
            MovementHandler movementHandler,
            SpecialActionsHandler specialActionsHandler)
        {
            _handlers = new List<(string, Func<long, string, CallbackQuery, Task>, bool)>();

            _handlers.Add((REFRESH_MAP, gameMenuHandler.HandleRefreshMap, false));
            _handlers.Add((SHOW_LOCATION, gameMenuHandler.HandleShowLocation, false));

            _handlers.Add((ATTACK_BOSS, battleHandler.HandleAttackBoss, false));
            _handlers.Add((DEFEND_BOSS, battleHandler.HandleDefendBoss, false));
            _handlers.Add((ABILITY_BOSS, battleHandler.HandleAbilityBoss, false));
            _handlers.Add((FLEE_BOSS, battleHandler.HandleFleeBoss, false));

            _handlers.Add((TAKE, inventoryHandler.HandleTake, true));
            _handlers.Add((EXAMINE, inventoryHandler.HandleExamine, true));
            _handlers.Add((USE, inventoryHandler.HandleUse, true));
            _handlers.Add((DROP, inventoryHandler.HandleDrop, true));

            _handlers.Add((MOVE, movementHandler.HandleMove, true));

            _handlers.Add((LEARN_LASER, specialActionsHandler.HandleLearnLaser, false));
            _handlers.Add((ATTACK_CRYSTAL, specialActionsHandler.HandleAttackCrystal, false));
        }

        public async Task HandleAsync(long chatId, string data, CallbackQuery callbackQuery)//обойти имеющийся список найти подходящий и вызвать метод Handl
        {
            foreach (var (key, handler, isPrefix) in _handlers)
            {
                if (isPrefix ? data.StartsWith(key) : data == key)
                {
                    await handler(chatId, data, callbackQuery);
                    return;
                }
            }
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Неизвестное действие");
        }

    }
}