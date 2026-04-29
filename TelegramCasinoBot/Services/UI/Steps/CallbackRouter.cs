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
        private readonly List<(string Key, Func<long, string, CallbackQuery, Task> Handler, bool IsPrefix)> _handlers;//_handlers заместо этого нужно передать список созданых шагов 
        private readonly TelegramBotClient _botClient;
        private readonly CharacterIconService _iconService;
        private readonly PlayerCreationUI _playerCreationUI;
        private readonly PlayerManager _playerManager;
        private readonly LocationService _locationService;
        private readonly InventoryService _inventoryService;
        private readonly MapService _mapService;
        private readonly BattleService _battleService;
        private readonly GameActionService _gameActionService;

        public const string SELECT_ICON = "select_icon_";
        public const string ICONS_PREV = "icons_prev";
        public const string ICONS_NEXT = "icons_next";
        public const string PREVIEW_ALL = "preview_all";
        public const string RACE = "race_";
        public const string CLASS = "class_";
        public const string CONFIRM_ICON = "confirm_icon";
        public const string CONFIRM_CHARACTER = "confirm_character";
        public const string RESTART_CHARACTER = "restart_character";
        public const string TAKE = "take_";
        public const string EXAMINE = "examine_";
        public const string USE = "use_";
        public const string DROP = "drop_";
        public const string MOVE = "move_";
        public const string REFRESH_MAP = "refresh_map";
        public const string SHOW_LOCATION = "show_location";
        public const string ATTACK_BOSS = "attack_boss";
        public const string DEFEND_BOSS = "defend_boss";
        public const string ABILITY_BOSS = "ability_boss";
        public const string FLEE_BOSS = "flee_boss";
        public const string LEARN_LASER = "learn_laser";
        public const string ATTACK_CRYSTAL = "attack_crystal";
        public const string NAME = "name";
        public const string GENDER = "gender";
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

            _handlers = new List<(string, Func<long, string, CallbackQuery, Task>, bool)>//обратиться к списку всех шагов начать их обходить, ключ
            {//поулчить обьект шага и в нем вызвать метод хендел
                (SELECT_ICON, HandleIconSelection, true),
                (ICONS_PREV, HandleIconSelection, false),
                (ICONS_NEXT, HandleIconSelection, false),
                (PREVIEW_ALL, HandleIconSelection, false),
                (RACE, HandleCreationCallback, true),
                (CLASS, HandleCreationCallback, true),
                (CONFIRM_ICON, HandleCreationCallback, false),
                (CONFIRM_CHARACTER, HandleCreationCallback, false),
                (RESTART_CHARACTER, HandleCreationCallback, false),
                (TAKE, HandleTake, true),
                (EXAMINE, HandleExamine, true),
                (USE, HandleUse, true),
                (DROP, HandleDrop, true),
                (MOVE, HandleMove, true),
                (REFRESH_MAP, HandleRefreshMap, false),
                (SHOW_LOCATION, HandleShowLocation, false),
                (ATTACK_BOSS, HandleAttackBoss, false),
                (DEFEND_BOSS, HandleDefendBoss, false),
                (ABILITY_BOSS, HandleAbilityBoss, false),
                (FLEE_BOSS, HandleFleeBoss, false),
                (LEARN_LASER, HandleLearnLaser, false),
                (ATTACK_CRYSTAL, HandleAttackCrystal, false),
            };
        }
        //переделать на метод хендел
        
        public async Task HandleAsync(long chatId, string data, CallbackQuery callbackQuery)
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