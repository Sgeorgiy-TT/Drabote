using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.DataStats;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.Gameplay
{
    public class GameActionService
    {
        private readonly TelegramBotClient _botClient;
        private readonly LocationService _locationService;
        private readonly PlayerService _playerService;
        private readonly InventoryService _inventoryService;
        private readonly MovementService _movementService;
        private readonly GameWorld _world;
        private readonly ILogger<GameActionService> _logger;
        private readonly ItemService _itemService;

        public GameActionService(
            TelegramBotClient botClient,
            LocationService locationService,
            PlayerService playerService,
            InventoryService inventoryService,
            MovementService movementService,
            GameWorld world,
            ILogger<GameActionService> logger,
            ItemService itemService)
        {
            _botClient = botClient;
            _locationService = locationService;
            _playerService = playerService;
            _inventoryService = inventoryService;
            _movementService = movementService;
            _world = world;
            _logger = logger;
            _itemService = itemService;
        }

        public async Task LearnLaserAbility(long chatId, Player player, CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало LearnLaserAbility для chatId {ChatId}", chatId);
            try
            {
                if (!player.AbilityNames.Contains("Лазерный луч"))
                {
                    player.AbilityNames.Add("Лазерный луч");
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Вы изучили Лазерный луч!");
                    await _locationService.ShowAbilityUnlockAnimation(chatId, "Лазерный луч", "🔮");
                    await _playerService.AddExperience(chatId, player, 75);

                    await _botClient.EditMessageTextAsync(
                        chatId: chatId,
                        messageId: callbackQuery.Message.MessageId,
                        text: $"*{_world.Locations[player.CurrentLocation].Name}*\n\nКристалл померк. Его энергия теперь течет в вас!",
                        parseMode: ParseMode.Markdown);

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Теперь вы можете пройти к Стражу Врат!",
                        replyMarkup: KeyboardHelper.GetEnhancedControls());
                }
            }
            finally
            {
                _logger.LogDebug("LearnLaserAbility завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task AttackCrystal(long chatId, Player player, CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало AttackCrystal для chatId {ChatId}", chatId);
            try
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "💥 Вы атаковали кристалл!");
                await _botClient.SendTextMessageAsync(chatId, "💥 Кристалл взрывается! Вы теряете 20 HP!");
                player.Health.Add(-20);

                if (player.Health.Current <= 0)
                {
                    player.Health.Current = 1;
                    await _botClient.SendTextMessageAsync(chatId, "😵 Вы едва выжили после взрыва!");
                }

                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: callbackQuery.Message.MessageId,
                    text: "*Кристальная Пещера*\n\nОбломки кристалла разбросаны по пещере. Энергия рассеяна.");
            }
            finally
            {
                _logger.LogDebug("AttackCrystal завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task UseItem(long chatId, Player player, CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало UseItem для chatId {ChatId}", chatId);
            try
            {
                var itemName = callbackQuery.Data.Substring(4);
                var item = _itemService.GetItemByName(itemName);
                if (item == null)
                {
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Предмет не найден");
                    return;
                }

                if (item.ItemType != "consumable")
                {
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Этот предмет нельзя использовать");
                    return;
                }

                if (!player.Inventory.Contains(item.Name))
                {
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ У вас нет этого предмета");
                    return;
                }

                string resultMessage = "";
                if (item.EffectType == "health")
                {
                    int heal = item.Value ?? 30;
                    player.Health.Current = Math.Min(player.Health.Max, player.Health.Current + heal);
                    resultMessage = $"💊 Вы использовали {item.Name} и восстановили {heal} HP.";
                }
                else if (item.EffectType == "mana")
                {
                    int mana = item.Value ?? 20;
                    player.Mana.Current = Math.Min(player.Mana.Max, player.Mana.Current + mana);
                    resultMessage = $"💙 Вы использовали {item.Name} и восстановили {mana} MP.";
                }
                else if (item.EffectType == "stamina")
                {
                    int stamina = item.Value ?? 20;
                    player.Stamina.Current = Math.Min(player.Stamina.Max, player.Stamina.Current + stamina);
                    resultMessage = $"💪 Вы использовали {item.Name} и восстановили {stamina} выносливости.";
                }
                else
                {
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Эффект предмета не поддерживается");
                    return;
                }

                player.Inventory.Remove(item.Name);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"✅ Использован: {item.Name}");
                await _botClient.SendTextMessageAsync(chatId, resultMessage);
                await _inventoryService.ShowInteractiveInventory(chatId, player);
            }
            finally
            {
                _logger.LogDebug("UseItem завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task DropItem(long chatId, Player player, CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало DropItem для chatId {ChatId}", chatId);
            try
            {
                var item = callbackQuery.Data.Substring(5);
                if (player.Inventory.Contains(item))
                {
                    player.Inventory.Remove(item);
                    var location = _world.Locations[player.CurrentLocation];
                    location.Items.Add(item);

                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"❌ Вы выбросили: {item}");
                    await _botClient.SendTextMessageAsync(chatId, $"🗑️ Вы выбросили {item}. Он остался в этой локации.");
                    await _inventoryService.ShowInteractiveInventory(chatId, player);
                }
            }
            finally
            {
                _logger.LogDebug("DropItem завершён для chatId {ChatId}", chatId);
            }
        }

        public async Task HandleInlineMovement(long chatId, Player player, CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало HandleInlineMovement для chatId {ChatId}", chatId);
            try
            {
                var direction = callbackQuery.Data.Substring(5);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"🔄 Перемещение: {direction}");
                await _movementService.ShowMovementAnimation(chatId, direction);
                bool moved = await _movementService.MovePlayer(player, direction);

                if (moved)
                {
                    await _locationService.DescribeLocation(chatId, player);
                    await _locationService.HandleLocationEvents(chatId, player);
                }
                else
                {
                    var currentLoc = _world.Locations[player.CurrentLocation];
                    GameLocation newLocation = direction.ToLower() switch
                    {
                        "север" or "north" => currentLoc.NorthLocation,
                        "юг" or "south" => currentLoc.SouthLocation,
                        "запад" or "west" => currentLoc.WestLocation,
                        "восток" or "east" => currentLoc.EastLocation,
                        _ => null
                    };

                    if (newLocation == null)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "❌ Туда нельзя пройти!",
                            replyMarkup: KeyboardHelper.GetEnhancedControls());
                    }
                    else if (newLocation.RequiredAbility != null && !player.AbilityNames.Contains(newLocation.RequiredAbility))
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            $"🚫 {newLocation.AccessDeniedMessage ?? $"Нужна способность: {newLocation.RequiredAbility}"}",
                            replyMarkup: KeyboardHelper.GetEnhancedControls());
                    }
                }
            }
            finally
            {
                _logger.LogDebug("HandleInlineMovement завершён для chatId {ChatId}", chatId);
            }
        }
    }
}