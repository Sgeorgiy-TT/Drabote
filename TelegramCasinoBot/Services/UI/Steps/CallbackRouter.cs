using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramCasinoBot.Services.Gameplay;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI.Dispatcher;
using TelegramCasinoBot.Services.UI.Handlers;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class CallbackRouter
    {
        private readonly CallbackDispatcher _dispatcher;
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

        public CallbackRouter(
        TelegramBotClient botClient,
        GameMenuHandler gameMenuHandler,
        BattleHandler battleHandler,
        InventoryHandler inventoryHandler,
        MovementHandler movementHandler,
        SpecialActionsHandler specialActionsHandler)
        {
            _dispatcher = new CallbackDispatcher(botClient);

            _dispatcher.Register(REFRESH_MAP, gameMenuHandler.HandleRefreshMap);
            _dispatcher.Register(SHOW_LOCATION, gameMenuHandler.HandleShowLocation);

            _dispatcher.Register(ATTACK_BOSS, battleHandler.HandleAttackBoss);
            _dispatcher.Register(DEFEND_BOSS, battleHandler.HandleDefendBoss);
            _dispatcher.Register(ABILITY_BOSS, battleHandler.HandleAbilityBoss);
            _dispatcher.Register(FLEE_BOSS, battleHandler.HandleFleeBoss);

            _dispatcher.Register(TAKE, inventoryHandler.HandleTake, true);
            _dispatcher.Register(EXAMINE, inventoryHandler.HandleExamine, true);
            _dispatcher.Register(USE, inventoryHandler.HandleUse, true);
            _dispatcher.Register(DROP, inventoryHandler.HandleDrop, true);

            _dispatcher.Register(MOVE, movementHandler.HandleMove, true);

            _dispatcher.Register(LEARN_LASER, specialActionsHandler.HandleLearnLaser);
            _dispatcher.Register(ATTACK_CRYSTAL, specialActionsHandler.HandleAttackCrystal);
        }

        public async Task HandleAsync(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _dispatcher.DispatchAsync(chatId, data, callbackQuery);
        }
    }
}