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
using TelegramCasinoBot.Models;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Models.Gameplay;
using TelegramCasinoBot.Models.Gameplay.Location;
using TelegramCasinoBot.Services.Callbacks;
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.JsonR;
using TelegramCasinoBot.Services.Models.DataStats;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI;
using TelegramCasinoBot.Utils;


namespace TelegramMetroidvaniaBot
{
    class Program
    {
        private static IServiceProvider _serviceProvider;
        private static ILogger<Program> _logger;
        private static TelegramBotClient _botClient;
        private static GameWorld _world;
        private static PlayerManager _playerManager = new PlayerManager();
        private static DatabaseService _databaseService;
        private static MenuServiceTG _menuService;
        private static MusicService _musicService;
        private static CharacterIconService _characterIconService;
        private static MovementService _movementService;
        private static LocationService _locationService;
        private static InventoryService _inventoryService;
        private static BattleService _battleService;
        private static CommandServiceTG _commandService;
        private static MapService _mapService;
        private static PlayerService _playerService;
        private static PlayerCreationUI _playerCreationUI;
        private static CallbackRouter _callbackRouter;

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

                string botToken = configuration["Bot:Token"];
                var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                _botClient = new TelegramBotClient(botToken, httpClient);
                services.AddSingleton(_botClient);

                ConfigureServices(services, configuration);

                _serviceProvider = services.BuildServiceProvider();

                _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();//в самом начале
                _logger.LogInformation("Запуск бота...");

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

            services.AddSingleton<IRaceService, JsonRaceRepository>();
            services.AddSingleton<IClassService, JsonClassRepository>();

            services.Configure<MapGeneratorOptions>(configuration.GetSection("MapGenerator"));
            services.Configure<ImageSettings>(configuration.GetSection("ImageSettings"));

            services.AddSingleton<ImageService>();
            services.AddSingleton<WorldFactory>();
            services.AddSingleton<GameWorld>(sp => sp.GetRequiredService<WorldFactory>().CreateWorld());
            services.AddSingleton<MapGeneratorService>();
            services.AddSingleton<PlayerManager>();

            services.AddSingleton<DatabaseService>();
            services.AddSingleton<MusicService>();
            services.AddSingleton<CharacterIconService>();
            services.AddSingleton<LocationService>();
            services.AddSingleton<MovementService>();
            services.AddSingleton<MapService>();
            services.AddSingleton<PlayerCreationUI>();
            services.AddSingleton<MenuServiceTG>();
            services.AddSingleton<InventoryService>();
            services.AddSingleton<PlayerService>();
            services.AddSingleton<BattleService>();
            services.AddSingleton<CommandServiceTG>();
        }

        private static void InitializeServices()
        {
            _logger.LogDebug("Начало InitializeServices");
            try
            {
                _logger.LogInformation("Инициализация сервисов...");

                _world = _serviceProvider.GetRequiredService<GameWorld>();
                _playerManager = _serviceProvider.GetRequiredService<PlayerManager>();
                _databaseService = _serviceProvider.GetRequiredService<DatabaseService>();
                _musicService = _serviceProvider.GetRequiredService<MusicService>();
                _characterIconService = _serviceProvider.GetRequiredService<CharacterIconService>();
                _locationService = _serviceProvider.GetRequiredService<LocationService>();
                _movementService = _serviceProvider.GetRequiredService<MovementService>();
                _mapService = _serviceProvider.GetRequiredService<MapService>();
                
                _playerCreationUI = _serviceProvider.GetRequiredService<PlayerCreationUI>();
                _menuService = _serviceProvider.GetRequiredService<MenuServiceTG>();
                _inventoryService = _serviceProvider.GetRequiredService<InventoryService>();
                _playerService = _serviceProvider.GetRequiredService<PlayerService>();
                _battleService = _serviceProvider.GetRequiredService<BattleService>();
                _commandService = _serviceProvider.GetRequiredService<CommandServiceTG>();
                _callbackRouter = new CallbackRouter(
                    _botClient,
                    _characterIconService,
                    _playerCreationUI,
                    _playerManager,
                    _locationService,
                    _inventoryService,
                    _mapService,
                    _battleService,
                    null
                );
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

                if (_playerCreationUI.IsInCharacterCreation(chatId))
                {
                    var playerInProgress = _playerCreationUI.GetCharacterInProgress(chatId);
                    if (playerInProgress != null)
                    {
                        if (string.IsNullOrEmpty(playerInProgress.Name))
                            await _playerCreationUI.HandleName(chatId, messageText);
                        else if (string.IsNullOrEmpty(playerInProgress.Gender))
                            await _playerCreationUI.HandleGender(chatId, messageText);
                    }
                    return;
                }

                if (messageText == "🚀 Новая игра" || messageText.ToLower() == "новая игра")
                {
                    _playerCreationUI.StartCreation(chatId);
                    return;
                }

                if (IsMenuCommand(messageText))
                {
                    await _menuService.HandleMenuCommand(chatId, messageText);
                    return;
                }

                Player player;
                if (!_playerManager.ContainsPlayer(chatId))
                {
                   var raceService = _serviceProvider.GetRequiredService<IRaceService>();
                   var classService = _serviceProvider.GetRequiredService<IClassService>();
                   var save = await _databaseService.GetPlayerSaveAsync(chatId);
                    if (save != null)
                    {
                        player = await LoadPlayerFromSave(save, raceService, classService);
                        _playerManager.AddOrUpdatePlayer(player);
                    }
                    else
                    {
                        player = new Player(chatId, null, null, null, null, null);
                        _playerManager.AddOrUpdatePlayer(player);
                    }
                }
                else
                {
                    player = _playerManager.GetPlayer(chatId);
                }
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
        //создать класс который будет абстрактным который будет наследником всех Ask и Handl - что- то связаное с обьектами, создавать обьект у которого есть метод хендел,циклом,//асицаативный массив
        static async Task HandleCallbackQuery(CallbackQuery callbackQuery)
        {
            _logger.LogDebug("Начало HandleCallbackQuery для callback {CallbackId}", callbackQuery.Id);
            var chatId = callbackQuery.Message.Chat.Id;
            var data = callbackQuery.Data;

            await _callbackRouter.HandleAsync(chatId, data, callbackQuery);
        }

        private static async Task<Player> LoadPlayerFromSave(PlayerSave save, IRaceService raceService, IClassService classService)
        {
            var race = await raceService.GetRaceByNameAsync(save.Race);
            var playerClass = await classService.GetClassByNameAsync(save.Class);

            var player = new Player(save.ChatId, save.PlayerName, save.Gender, race, playerClass, null, save.Experience, save.Level, save.CurrentLocation, 5, 5);
            player.Health.Current = save.Health;
            player.Mana.Current = save.Mana;
            player.Stamina.Current = save.Stamina;
            player.CurrentLocation = save.CurrentLocation;
            player.Experience = save.Experience;
            player.Level = save.Level;
           
            return player;
        }

        private static bool IsMenuCommand(string messageText)
        {
            var menuCommands = new[] {
                "🎮 продолжить", "продолжить",
                
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