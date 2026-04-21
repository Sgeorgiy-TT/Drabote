using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramCasinoBot.Services.Gameplay;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI;
using TelegramMetroidvaniaBot;

namespace TelegramCasinoBot.Services.Callbacks
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
        private readonly Program _program;
        private readonly GameActionService _gameActionService;

        private const string SELECT_ICON = "select_icon_";
        private const string ICONS_PREV = "icons_prev";
        private const string ICONS_NEXT = "icons_next";
        private const string PREVIEW_ALL = "preview_all";
        private const string RACE = "race_";
        private const string CLASS = "class_";
        private const string CONFIRM_ICON = "confirm_icon";
        private const string CONFIRM_CHARACTER = "confirm_character";
        private const string RESTART_CHARACTER = "restart_character";
        private const string TAKE = "take_";
        private const string EXAMINE = "examine_";
        private const string USE = "use_";
        private const string DROP = "drop_";
        private const string MOVE = "move_";
        private const string REFRESH_MAP = "refresh_map";
        private const string SHOW_LOCATION = "show_location";
        private const string ATTACK_BOSS = "attack_boss";
        private const string DEFEND_BOSS = "defend_boss";
        private const string ABILITY_BOSS = "ability_boss";
        private const string FLEE_BOSS = "flee_boss";
        private const string LEARN_LASER = "learn_laser";
        private const string ATTACK_CRYSTAL = "attack_crystal";

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
                [RACE] = HandleRace,
                [CLASS] = HandleClass,
                [CONFIRM_ICON] = HandleConfirmIcon,
                [CONFIRM_CHARACTER] = HandleConfirmCharacter,
                [RESTART_CHARACTER] = HandleRestartCharacter,
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
            string key = GetKey(data);
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
            if (data == ICONS_PREV || data == ICONS_NEXT || data == PREVIEW_ALL)
                return data;
            if (data.StartsWith(SELECT_ICON)) return SELECT_ICON;
            if (data.StartsWith(RACE)) return RACE;
            if (data.StartsWith(CLASS)) return CLASS;
            if (data == CONFIRM_ICON) return CONFIRM_ICON;
            if (data == CONFIRM_CHARACTER) return CONFIRM_CHARACTER;
            if (data == RESTART_CHARACTER) return RESTART_CHARACTER;
            if (data.StartsWith(TAKE)) return TAKE;
            if (data.StartsWith(EXAMINE)) return EXAMINE;
            if (data.StartsWith(USE)) return USE;
            if (data.StartsWith(DROP)) return DROP;
            if (data.StartsWith(MOVE)) return MOVE;
            if (data == REFRESH_MAP) return REFRESH_MAP;
            if (data == SHOW_LOCATION) return SHOW_LOCATION;
            if (data == ATTACK_BOSS) return ATTACK_BOSS;
            if (data == DEFEND_BOSS) return DEFEND_BOSS;
            if (data == ABILITY_BOSS) return ABILITY_BOSS;
            if (data == FLEE_BOSS) return FLEE_BOSS;
            if (data == LEARN_LASER) return LEARN_LASER;
            if (data == ATTACK_CRYSTAL) return ATTACK_CRYSTAL;
            return null;
        }

        private async Task HandleIconSelection(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _iconService.HandleIconSelection(chatId, data);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private async Task HandleRace(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _playerCreationUI.HandleRace(chatId, data);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private async Task HandleClass(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _playerCreationUI.HandleClass(chatId, data);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
        }

        private async Task HandleConfirmIcon(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _playerCreationUI.HandleIconConfirmation(chatId);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Иконка подтверждена!");
        }

        private async Task HandleConfirmCharacter(long chatId, string data, CallbackQuery callbackQuery)
        {
            await _playerCreationUI.CompleteCreation(chatId);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Персонаж создан!");
            var createdPlayer = _playerManager.GetPlayer(chatId);
            if (createdPlayer != null)
                await _locationService.DescribeLocation(chatId, createdPlayer);
        }

        private async Task HandleRestartCharacter(long chatId, string data, CallbackQuery callbackQuery)
        {
            _playerCreationUI.StartCreation(chatId);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🔄 Начинаем заново...");
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