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
using TelegramCasinoBot.Services.UI.Steps.Dispatcher;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class CallbackRouter
    {
        private readonly CallbackDispatcher _dispatcher;
        private readonly PlayerCreationUI _playerCreationUI;
        private readonly TelegramBotClient _botClient;

        public const string REFRESH_MAP = "refresh_map";
        public const string SHOW_LOCATION = "show_location";
        public const string MOB_ATTACK = "mob_attack";
        public const string MOB_DEFEND = "mob_defend";
        public const string MOB_ABILITY = "mob_ability";
        public const string MOB_ITEM = "mob_item";
        public const string MOB_FLEE = "mob_flee";
        public const string ABILITY_SELECT = "ability_select_";
        public const string ITEM_USE = "item_use_";
        public const string BACK_TO_BATTLE = "back_to_battle";
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
            SpecialActionsHandler specialActionsHandler,
            PlayerCreationUI playerCreationUI)
        {
            _botClient = botClient;
            _playerCreationUI = playerCreationUI;
            _dispatcher = new CallbackDispatcher(botClient);

            _dispatcher.Register(REFRESH_MAP, gameMenuHandler.HandleRefreshMap);
            _dispatcher.Register(SHOW_LOCATION, gameMenuHandler.HandleShowLocation);

            _dispatcher.Register(MOB_ATTACK, battleHandler.HandleMobAction);
            _dispatcher.Register(MOB_DEFEND, battleHandler.HandleMobAction);
            _dispatcher.Register(MOB_ABILITY, battleHandler.HandleMobAction);
            _dispatcher.Register(MOB_ITEM, battleHandler.HandleMobAction);
            _dispatcher.Register(MOB_FLEE, battleHandler.HandleMobAction);
            _dispatcher.Register(ABILITY_SELECT, battleHandler.HandleAbilitySelect, true);
            _dispatcher.Register(ITEM_USE, battleHandler.HandleItemUse, true);
            _dispatcher.Register(BACK_TO_BATTLE, battleHandler.HandleBackToBattle);
            _dispatcher.Register("icon_back", HandleIconBack, false);
            _dispatcher.Register(ATTACK_BOSS, battleHandler.HandleMobAction);
            _dispatcher.Register(DEFEND_BOSS, battleHandler.HandleMobAction);
            _dispatcher.Register(ABILITY_BOSS, battleHandler.HandleMobAction);
            _dispatcher.Register(FLEE_BOSS, battleHandler.HandleMobAction);
            _dispatcher.Register("equip_weapon", inventoryHandler.HandleEquipWeapon, false);
            _dispatcher.Register("equip_armor", inventoryHandler.HandleEquipArmor, false);
            _dispatcher.Register("equip_select_", inventoryHandler.HandleEquipSelect, true);
            _dispatcher.Register("unequip_weapon", inventoryHandler.HandleUnequipWeapon, false);
            _dispatcher.Register("unequip_armor", inventoryHandler.HandleUnequipArmor, false);
            _dispatcher.Register("equipment_back", inventoryHandler.HandleEquipmentBack, false);
            _dispatcher.Register("show_equipment", inventoryHandler.HandleEquipmentMenu, false);
            _dispatcher.Register("settings_speed", gameMenuHandler.HandleSpeedMenu, false);
            _dispatcher.Register("settings_back", gameMenuHandler.HandleSettingsBack, false);
            _dispatcher.Register("speed_", gameMenuHandler.HandleSpeedSelect, true);
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
        private async Task HandleIconBack(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _playerCreationUI.HandleInput(chatId, "back");
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }
    }
}