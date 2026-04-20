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
                ["select_icon_"] = HandleIconSelection,
                ["icons_prev"] = HandleIconSelection,
                ["icons_next"] = HandleIconSelection,
                ["preview_all"] = HandleIconSelection,
                ["race_"] = HandleRace,
                ["class_"] = HandleClass,
                ["confirm_icon"] = HandleConfirmIcon,
                ["confirm_character"] = HandleConfirmCharacter,
                ["restart_character"] = HandleRestartCharacter,
                ["take_"] = HandleTake,
                ["examine_"] = HandleExamine,
                ["use_"] = HandleUse,
                ["drop_"] = HandleDrop,
                ["move_"] = HandleMove,
                ["refresh_map"] = HandleRefreshMap,
                ["show_location"] = HandleShowLocation,
                ["attack_boss"] = HandleAttackBoss,
                ["defend_boss"] = HandleDefendBoss,
                ["ability_boss"] = HandleAbilityBoss,
                ["flee_boss"] = HandleFleeBoss,
                ["learn_laser"] = HandleLearnLaser,
                ["attack_crystal"] = HandleAttackCrystal,
            };
        }

        public async Task HandleAsync(long chatId, string data, CallbackQuery callbackQuery)
        {
            var key = _handlers.Keys.FirstOrDefault(k => data.StartsWith(k));
            if (key != null && _handlers.TryGetValue(key, out var handler))
            {
                await handler(chatId, data, callbackQuery);
            }
            else
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Неизвестное действие");
            }
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