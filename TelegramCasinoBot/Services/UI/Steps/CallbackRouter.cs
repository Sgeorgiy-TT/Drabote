using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramCasinoBot.Services.Gameplay;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI;

namespace TelegramCasinoBot.Services.UI.Steps
{
    public class CallbackRouter
    {
        private readonly Dictionary<string, Func<long, string, CallbackQuery, Task>> _handlers;
        private readonly TelegramBotClient _botClient;
        private readonly CharacterIconService _iconService;
        private readonly PlayerCreationUI _playerCreationUI;
        private readonly PlayerManager _playerManager;
        private readonly LocationService _locationService;
        private readonly InventoryService _inventoryService;
        private readonly MapService _mapService;
        private readonly BattleService _battleService;
        private readonly GameActionService _gameActionService;

        public const string SELECT_ICON = "select_icon:";//между ключем и разделителем
        public const string ICONS_PREV = "icons_prev:";
        public const string ICONS_NEXT = "icons_next:";
        public const string PREVIEW_ALL = "preview_all:";
        public static const string KEY_GENDER = "gender";//попробавать реализовать через обькт
        public const string RACE = "race:";
        public const string CLASS = "class:";
        public const string CONFIRM_ICON = "confirm_icon:";
        public const string CONFIRM_CHARACTER = "confirm_character:";
        public const string RESTART_CHARACTER = "restart_character:";
        public const string TAKE = "take:";
        public const string EXAMINE = "examine:";
        public const string USE = "use:";
        public const string DROP = "drop:";
        public const string MOVE = "move:";
        public const string REFRESH_MAP = "refresh_map:";
        public const string SHOW_LOCATION = "show_location:";
        public const string ATTACK_BOSS = "attack_boss:";
        public const string DEFEND_BOSS = "defend_boss:";
        public const string ABILITY_BOSS = "ability_boss:";
        public const string FLEE_BOSS = "flee_boss:";
        public const string LEARN_LASER = "learn_laser:";
        public const string ATTACK_CRYSTAL = "attack_crystal:";

        public CallbackRouter(
            TelegramBotClient botClient,
            CharacterIconService iconService,
            PlayerCreationUI playerCreationUI,
            PlayerManager playerManager,
            LocationService locationService,
            InventoryService inventoryService,
            MapService mapService,
            BattleService battleService,
            GameActionService gameActionService)
        {
            _botClient = botClient;
            _iconService = iconService;
            _playerCreationUI = playerCreationUI;
            _playerManager = playerManager;
            _locationService = locationService;
            _inventoryService = inventoryService;
            _mapService = mapService;
            _battleService = battleService;
            _gameActionService = gameActionService;

            _handlers = new Dictionary<string, Func<long, string, CallbackQuery, Task>>
            {
                [SELECT_ICON] = HandleIconSelection,
                [ICONS_PREV] = HandleIconSelection,
                [ICONS_NEXT] = HandleIconSelection,
                [PREVIEW_ALL] = HandleIconSelection,
                [RACE] = HandleCreationCallback,
                [CLASS] = HandleCreationCallback,
                [CONFIRM_ICON] = HandleCreationCallback,
                [CONFIRM_CHARACTER] = HandleCreationCallback,
                [RESTART_CHARACTER] = HandleCreationCallback,
                [TAKE] = HandleTake,
                [EXAMINE] = HandleExamine,
                [USE] = HandleUse,
                [DROP] = HandleDrop,
                [MOVE] = HandleMove,
                [REFRESH_MAP] = HandleRefreshMap,
                [SHOW_LOCATION] = HandleShowLocation,
                [ATTACK_BOSS] = HandleAttackBoss,
                [DEFEND_BOSS] = HandleDefendBoss,
                [ABILITY_BOSS] = HandleAbilityBoss,
                [FLEE_BOSS] = HandleFleeBoss,
                [LEARN_LASER] = HandleLearnLaser,
                [ATTACK_CRYSTAL] = HandleAttackCrystal,
            };
        }

        public async Task HandleAsync(long chatId, string data, CallbackQuery callbackQuery)
        {
            string key = GetKey(data);//где заполняеться
            if (key != null && _handlers.TryGetValue(key, out var handler))
            {
                await handler(chatId, data, callbackQuery);
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Неизвестное действие");
            }
        }

        private string GetKey(string data)
        {
            return data switch
            {
                ICONS_PREV => ICONS_PREV,
                ICONS_NEXT => ICONS_NEXT,
                PREVIEW_ALL => PREVIEW_ALL,
                CONFIRM_ICON => CONFIRM_ICON,
                CONFIRM_CHARACTER => CONFIRM_CHARACTER,
                RESTART_CHARACTER => RESTART_CHARACTER,
                REFRESH_MAP => REFRESH_MAP,
                SHOW_LOCATION => SHOW_LOCATION,
                ATTACK_BOSS => ATTACK_BOSS,
                DEFEND_BOSS => DEFEND_BOSS,
                ABILITY_BOSS => ABILITY_BOSS,
                FLEE_BOSS => FLEE_BOSS,
                LEARN_LASER => LEARN_LASER,
                ATTACK_CRYSTAL => ATTACK_CRYSTAL,
                _ when data.StartsWith(SELECT_ICON) => SELECT_ICON,
                _ when data.StartsWith(RACE) => RACE,
                _ when data.StartsWith(CLASS) => CLASS,
                _ when data.StartsWith(TAKE) => TAKE,
                _ when data.StartsWith(EXAMINE) => EXAMINE,
                _ when data.StartsWith(USE) => USE,
                _ when data.StartsWith(DROP) => DROP,
                _ when data.StartsWith(MOVE) => MOVE,
                _ => null
            };
        }
        //иметь список обьктов, которые еще не обьявлены, //их надо будет обходить
        private async Task HandleCreationCallback(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _playerCreationUI.HandleInput(chatId, data);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private async Task HandleIconSelection(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _iconService.HandleIconSelection(chatId, data);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private async Task HandleTake(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _inventoryService.HandleItemPickup(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleExamine(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _inventoryService.HandleItemExamine(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleUse(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _gameActionService.UseItem(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleDrop(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _gameActionService.DropItem(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleMove(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _gameActionService.HandleInlineMovement(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleRefreshMap(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🗺️ Карта обновлена");
                await _mapService.ShowInteractiveMap(chatId, player);
            }
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleShowLocation(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "📍 Текущая локация");
                await _locationService.DescribeLocation(chatId, player);
            }
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleAttackBoss(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _battleService.HandleBossBattle(chatId, player, callbackQuery.Message.MessageId);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleDefendBoss(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _battleService.HandleBossDefense(chatId, player, callbackQuery.Message.MessageId);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleAbilityBoss(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _battleService.HandleBossAbility(chatId, player, callbackQuery.Message.MessageId);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleFleeBoss(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _battleService.HandleBossFlee(chatId, player, callbackQuery.Message.MessageId);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleLearnLaser(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _gameActionService.LearnLaserAbility(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }

        private async Task HandleAttackCrystal(long chatId, string data, CallbackQuery callbackQuery)
        {
            var player = _playerManager.GetPlayer(chatId);
            if (player != null)
                await _gameActionService.AttackCrystal(chatId, player, callbackQuery);
            else
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден");
        }
    }
}