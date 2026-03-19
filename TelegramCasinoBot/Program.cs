using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Models.Data;
using TelegramCasinoBot.Services.Models.DataStats;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI;
using TelegramCasinoBot.Utils;
using TelegramMetroidvaniaBot.Models;


namespace TelegramMetroidvaniaBot
{
    class Program
    {
        private static IServiceProvider _serviceProvider;
        private static ILogger<Program> _logger;
        private static TelegramBotClient _botClient;
        private static readonly Dictionary<long, Player> _players = new Dictionary<long, Player>();
        private static GameWorld _world;
        private static DatabaseService _databaseService;
        private static MenuService _menuService;
        private static MusicService _musicService;
        private static CharacterCreationService _characterCreationService;
        private static CharacterIconService _characterIconService;
        private static MovementService _movementService;
        private static LocationService _locationService;
        private static InventoryService _inventoryService;
        private static BattleService _battleService;
        private static CommandService _commandService;
        private static MapService _mapService;
        private static PlayerService _playerService;

        public static GameWorld World => _world;
        public static Dictionary<long, Player> Players => _players;

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("Logs/log-.txt",
                              rollingInterval: RollingInterval.Day,
                              outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Запуск приложения");

                var basePath = Directory.GetCurrentDirectory();
                if (basePath.EndsWith("bin\\Debug\\net5.0") || basePath.EndsWith("bin/Debug/net5.0"))
                {
                    basePath = Directory.GetParent(basePath).Parent.Parent.FullName;
                }

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var services = new ServiceCollection();
                ConfigureServices(services, configuration);
                _serviceProvider = services.BuildServiceProvider();

                _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
                _logger.LogInformation("Запуск бота...");

                string botToken = configuration["Bot:Token"];
                BotClientHolder.Initialize(botToken);
                _botClient = BotClientHolder.Client;
                _logger.LogInformation("TelegramBotClient инициализирован");

                _world = new GameWorld();
                _logger.LogInformation("GameWorld инициализирован");

                InitializeServices();

                _logger.LogInformation("Все сервисы инициализированы. Начало Polling...");
                await StartPolling();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Приложение завершилось с ошибкой");
            }
            finally
            {
                Log.CloseAndFlush();
                _logger?.LogDebug("Main завершён");
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(configuration);

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            });

            services.AddSingleton<IRaceService, RaceService>();
            services.AddSingleton<IClassService, ClassService>();
            services.Configure<MapGeneratorOptions>(configuration.GetSection("MapGenerator"));
        }

        private static void InitializeServices()
        {
            _logger.LogDebug("Начало InitializeServices");
            try
            {
                _logger.LogInformation("Инициализация сервисов...");

                var raceService = _serviceProvider.GetRequiredService<IRaceService>();
                var classService = _serviceProvider.GetRequiredService<IClassService>();

                var worldFactory = new WorldFactory();
                _world = worldFactory.CreateWorld();

                var mapGeneratorLogger = _serviceProvider.GetRequiredService<ILogger<MapGeneratorService>>();
                
                var mapGeneratorOptions = _serviceProvider.GetRequiredService<IOptions<MapGeneratorOptions>>();
                var mapGenerator = new MapGeneratorService(mapGeneratorLogger, mapGeneratorOptions);
                _databaseService = new DatabaseService(_serviceProvider.GetRequiredService<ILogger<DatabaseService>>());
                _musicService = new MusicService(_botClient, _serviceProvider.GetRequiredService<ILogger<MusicService>>());
                _characterIconService = new CharacterIconService(_botClient, _serviceProvider.GetRequiredService<ILogger<CharacterIconService>>());

                _locationService = new LocationService(_botClient, _world, mapGenerator,
                    _serviceProvider.GetRequiredService<ILogger<LocationService>>());
                _movementService = new MovementService(_botClient, _world, _locationService,
                    _serviceProvider.GetRequiredService<ILogger<MovementService>>());
                _mapService = new MapService(_botClient, _world,
                    _serviceProvider.GetRequiredService<ILogger<MapService>>());

                _characterCreationService = new CharacterCreationService(
                    _botClient,
                    _databaseService,
                    _characterIconService,
                    raceService,
                    classService,
                    _locationService,  
                    _world,
                    _serviceProvider.GetRequiredService<ILogger<CharacterCreationService>>());

                _menuService = new MenuService(_botClient, _databaseService, _musicService,
                    _characterCreationService, _serviceProvider.GetRequiredService<ILogger<MenuService>>());
                _inventoryService = new InventoryService(_botClient, _world,
                    _serviceProvider.GetRequiredService<ILogger<InventoryService>>());
                _playerService = new PlayerService(_botClient, _world,
                    _serviceProvider.GetRequiredService<ILogger<PlayerService>>());
                _battleService = new BattleService(_botClient, _world, _locationService, _playerService,
                    _serviceProvider.GetRequiredService<ILogger<BattleService>>());
                _commandService = new CommandService(_botClient, _world, _movementService, _locationService,
                    _mapService, _inventoryService, _serviceProvider.GetRequiredService<ILogger<CommandService>>());
            }
            finally
            {
                _logger.LogDebug("InitializeServices завершён");
            }
        }

        static async Task StartPolling()
        {
            _logger.LogDebug("Начало StartPolling");
            int offset = 0;
            while (true)
            {
                try
                {
                    var updates = await _botClient.GetUpdatesAsync(offset, limit: 100, timeout: 30);
                    foreach (var update in updates)
                    {
                        await HandleUpdateAsync(update);
                        offset = update.Id + 1;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в StartPolling: {Message}", ex.Message);
                    await Task.Delay(1000);
                }
            }
        }

        static async Task HandleUpdateAsync(Update update)
        {
            _logger.LogDebug("Начало HandleUpdateAsync для обновления {UpdateId}", update.Id);
            try
            {
                _logger.LogDebug("Обработка обновления {UpdateId} типа {Type}", update.Id, update.Type);

                if (update.CallbackQuery != null)
                {
                    await HandleCallbackQuery(update.CallbackQuery);
                    return;
                }

                if (update.Message is not { } message || message.Text is not { } messageText)
                    return;

                var chatId = message.Chat.Id;

                if (messageText == "/start" || messageText.ToLower() == "меню")
                {
                    await _menuService.ShowMainMenu(chatId);
                    return;
                }

                if (_characterCreationService.IsInCharacterCreation(chatId))
                {
                    await _menuService.HandleMenuCommand(chatId, messageText);
                    return;
                }

                if (IsMenuCommand(messageText))
                {
                    await _menuService.HandleMenuCommand(chatId, messageText);
                    return;
                }

                if (!_players.ContainsKey(chatId))
                {
                    var save = await _databaseService.GetPlayerSaveAsync(chatId);
                    if (save != null)
                    {
                        _players[chatId] = LoadPlayerFromSave(save);
                    }
                    else
                    {
                        _players[chatId] = new Player(chatId);
                    }
                }

                var player = _players[chatId];
                await _commandService.HandleCommand(chatId, player, messageText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Необработанная ошибка в HandleUpdateAsync для обновления {UpdateId}", update.Id);
            }
            finally
            {
                _logger.LogDebug("HandleUpdateAsync завершён для обновления {UpdateId}", update.Id);
            }
        }

        static async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало HandleCallbackQuery для callback {CallbackId}", callbackQuery.Id);
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            if (data.StartsWith("select_icon_") || data == "icons_prev" || data == "icons_next" || data == "preview_all")
            {
                await _characterIconService.HandleIconSelection(chatId, data);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                return;
            }
            else if (data == "confirm_icon")
            {
                await _characterCreationService.HandleIconConfirmation(chatId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Иконка подтверждена!");
                return;
            }
            else if (data == "change_icon")
            {
                if (_characterCreationService.IsInCharacterCreation(chatId))
                {
                    var playerInProgress = _characterCreationService.GetCharacterInProgress(chatId);
                    if (playerInProgress != null)
                    {
                        await _characterIconService.StartIconSelection(chatId, playerInProgress.Gender, playerInProgress.Race);
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🔄 Выберите другую иконку");
                    }
                }
                return;
            }

            if (data.StartsWith("race_"))
            {
                var raceId = data.Substring(5);
                await _characterCreationService.HandleRaceSelection(chatId, raceId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                return;
            }
            else if (data.StartsWith("class_"))
            {
                var classId = data.Substring(6);
                await _characterCreationService.HandleClassSelection(chatId, classId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                return;
            }
            else if (data == "confirm_character")
            {
                await _characterCreationService.CompleteCharacterCreation(chatId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Персонаж создан!");
                return;
            }
            else if (data == "restart_character")
            {
                await _characterCreationService.StartCharacterCreation(chatId);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🔄 Начинаем заново...");
                return;
            }

            if (!_players.ContainsKey(chatId))
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Игрок не найден!");
                return;
            }

            var player = _players[chatId];

            try
            {
                if (callbackQuery.Data.StartsWith("take_"))
                {
                    await _inventoryService.HandleItemPickup(chatId, player, callbackQuery);
                }
                else if (callbackQuery.Data.StartsWith("examine_"))
                {
                    await _inventoryService.HandleItemExamine(chatId, player, callbackQuery);
                }
                else if (callbackQuery.Data == "learn_laser")
                {
                    await LearnLaserAbility(chatId, player, callbackQuery);
                }
                else if (callbackQuery.Data == "attack_crystal")
                {
                    await AttackCrystal(chatId, player, callbackQuery);
                }
                else if (callbackQuery.Data.StartsWith("use_"))
                {
                    await UseItem(chatId, player, callbackQuery);
                }
                else if (callbackQuery.Data.StartsWith("drop_"))
                {
                    await DropItem(chatId, player, callbackQuery);
                }
                else if (callbackQuery.Data.StartsWith("move_"))
                {
                    await HandleInlineMovement(chatId, player, callbackQuery);
                }
                else if (callbackQuery.Data == "refresh_map")
                {
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "🗺️ Карта обновлена");
                    await _mapService.ShowInteractiveMap(chatId, player);
                }
                else if (callbackQuery.Data == "show_location")
                {
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "📍 Текущая локация");
                    await _locationService.DescribeLocation(chatId, player);
                }
                else if (callbackQuery.Data == "attack_boss")
                {
                    await _battleService.HandleBossBattle(chatId, player, callbackQuery.Message.MessageId);
                }
                else if (callbackQuery.Data == "defend_boss")
                {
                    await _battleService.HandleBossDefense(chatId, player, callbackQuery.Message.MessageId);
                }
                else if (callbackQuery.Data == "ability_boss")
                {
                    await _battleService.HandleBossAbility(chatId, player, callbackQuery.Message.MessageId);
                }
                else if (callbackQuery.Data == "flee_boss")
                {
                    await _battleService.HandleBossFlee(chatId, player, callbackQuery.Message.MessageId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в HandleCallbackQuery: {Message}", ex.Message);
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Произошла ошибка");
            }
            finally
            {
                _logger.LogDebug("HandleCallbackQuery завершён для callback {CallbackId}", callbackQuery.Id);
            }
        }

        private static Player LoadPlayerFromSave(PlayerSave save)
        {
            var player = new Player(save.ChatId);
                                                 
            player.Name = save.PlayerName;
            player.CurrentLocation = save.CurrentLocation;
            player.Health = save.Health;
            player.MaxHealth = save.MaxHealth;
            player.Mana = save.Mana;
            player.MaxMana = save.MaxMana;
            player.Experience = save.Experience;
            player.Level = save.Level;
            player.Race = save.Race;
            player.Class = save.Class;
            player.Gender = save.Gender;
            
            return player;
        }

        private static bool IsMenuCommand(string messageText)
        {
            var menuCommands = new[] {
                "🎮 продолжить", "продолжить",
                "🚀 новая игра", "новая игра",
                "💾 загрузить", "загрузить",
                "⚙️ настройки", "настройки",
                "🎵 стоп музыка", "стоп музыка",
                "🎵 старт музыка", "старт музыка",
                "❌ выход", "выход"
            };
            return menuCommands.Contains(messageText.ToLower());
        }

        static async Task LearnLaserAbility(long chatId, Player player, CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало LearnLaserAbility для chatId {ChatId}", chatId);
            try
            {
                if (!player.Abilities.Contains("Лазерный луч"))
                {
                    player.Abilities.Add("Лазерный луч");
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

        static async Task AttackCrystal(long chatId, Player player, CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало AttackCrystal для chatId {ChatId}", chatId);
            try
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "💥 Вы атаковали кристалл!");
                var sentMsg = await _botClient.SendTextMessageAsync(chatId, "💥 Кристалл взрывается! Вы теряете 20 HP!");
                player.Health -= 20;

                if (player.Health <= 0)
                {
                    player.Health = 1;
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

        static async Task UseItem(long chatId, Player player, CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало UseItem для chatId {ChatId}", chatId);
            try
            {
                var item = callbackQuery.Data.Substring(4);
                var result = item switch
                {
                    "Древний артефакт" => "💎 Артефакт излучает теплую энергию, но ничего не происходит...",
                    "Ключ от ворот" => "🔑 Ключ тяжелый и холодный. Он подходит только к вратам в Зале Стражей.",
                    _ => $"🎒 Вы используете {item}, но эффекта нет."
                };

                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, $"✅ Использован: {item}");
                await _botClient.SendTextMessageAsync(chatId, result);
            }
            finally
            {
                _logger.LogDebug("UseItem завершён для chatId {ChatId}", chatId);
            }
        }

        static async Task DropItem(long chatId, Player player, CallbackQuery callbackQuery)
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

        static async Task HandleInlineMovement(long chatId, Player player, CallbackQuery callbackQuery)
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
                    else if (newLocation.RequiredAbility != null && !player.Abilities.Contains(newLocation.RequiredAbility))
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
        public static class BotClientHolder
        {
            public static TelegramBotClient Client { get; private set; }
            public static string Token { get; private set; }
            private static HttpClient _httpClient;

            public static void Initialize(string token)
            {
                Token = token;
                _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                Client = new TelegramBotClient(token, _httpClient);
            }

            public static async Task<bool> ReconnectAsync()
            {
                try
                {
                    var newClient = new TelegramBotClient(Token, _httpClient);
                  
                    await newClient.GetMeAsync();
                    Client = newClient;
                    return true;
                }
                catch (Exception ex)
                {
                    
                    return false;
                }
            }
        }
    }
}