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
using TelegramCasinoBot.Services.Data;
using TelegramCasinoBot.Services.Gameplay;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.Infrastructure.Location;
using TelegramCasinoBot.Services.JsonR;
using TelegramCasinoBot.Services.Models.Data.Creation;
using TelegramCasinoBot.Services.Models.Data.Gameplay;
using TelegramCasinoBot.Services.Models.DataStats;
using TelegramCasinoBot.Services.Models.Gameplay;
using TelegramCasinoBot.Services.Models.Gameplay.Location;
using TelegramCasinoBot.Services.UI;
using TelegramCasinoBot.Services.UI.Handlers;
using TelegramCasinoBot.Services.UI.Steps;
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

                string botToken = "8448828956:AAEBbcQAMbBpFKC1iKIdC86m_fYMOsWEf40";
                var botController = new TelegramBotController(botToken);
                services.AddSingleton(botController);
                services.AddSingleton(sp => sp.GetRequiredService<TelegramBotController>().Client);

                ConfigureServices(services, configuration);

                _serviceProvider = services.BuildServiceProvider();

                _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
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

            services.AddSingleton<AbilityService>();
            services.AddSingleton<ItemService>();
            services.AddSingleton<MobService>();
            services.AddSingleton<MobSpawnService>();

            services.AddSingleton<ImageService>();
            services.AddSingleton<WorldFactory>();
            services.AddSingleton(sp => sp.GetRequiredService<TelegramBotController>().Client);
            services.AddSingleton<MapGeneratorService>();
            services.AddSingleton<PlayerManager>();
            services.AddSingleton<GameMenuHandler>();
            services.AddSingleton<BattleHandler>();
            services.AddSingleton<InventoryHandler>();
            services.AddSingleton<MovementHandler>();
            services.AddSingleton<SpecialActionsHandler>();

            services.AddSingleton<CallbackRouter>(sp =>
            {
                return new CallbackRouter(
                    sp.GetRequiredService<TelegramBotClient>(),
                    sp.GetRequiredService<GameMenuHandler>(),
                    sp.GetRequiredService<BattleHandler>(),
                    sp.GetRequiredService<InventoryHandler>(),
                    sp.GetRequiredService<MovementHandler>(),
                    sp.GetRequiredService<SpecialActionsHandler>()
                );
            });
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<MusicService>();
            services.AddSingleton<CharacterIconService>();
            services.AddSingleton<LocationService>();
            services.AddSingleton<MovementService>();
            services.AddSingleton<MapService>();
            services.AddSingleton<MenuServiceTG>();
            services.AddSingleton<InventoryService>();
            services.AddSingleton<PlayerService>();
            services.AddSingleton<BattleService>();
            services.AddSingleton<CommandServiceTG>();
            services.AddSingleton<GameActionService>();
            services.AddSingleton(sp => {
                var factory = new WorldFactory();
                return factory.CreateWorld();
            });
            services.AddSingleton<PlayerCreationUI>(sp =>
            {
                return new PlayerCreationUI(
                    sp.GetRequiredService<TelegramBotClient>(),
                    sp.GetRequiredService<ILogger<PlayerCreationUI>>(),
                    sp.GetRequiredService<IRaceService>(),
                    sp.GetRequiredService<IClassService>(),
                    sp.GetRequiredService<CharacterIconService>(),
                    sp.GetRequiredService<DatabaseService>(),
                    sp.GetRequiredService<PlayerManager>(),
                    sp.GetRequiredService<AbilityService>(),
                    sp.GetRequiredService<ImageService>(),
                    sp.GetRequiredService<ILoggerFactory>()
                );
            });
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
                _botClient = _serviceProvider.GetRequiredService<TelegramBotClient>();

                _playerCreationUI = _serviceProvider.GetRequiredService<PlayerCreationUI>();
                _menuService = _serviceProvider.GetRequiredService<MenuServiceTG>();
                _inventoryService = _serviceProvider.GetRequiredService<InventoryService>();
                _playerService = _serviceProvider.GetRequiredService<PlayerService>();
                _battleService = _serviceProvider.GetRequiredService<BattleService>();
                _commandService = _serviceProvider.GetRequiredService<CommandServiceTG>();
                var gameActionService = _serviceProvider.GetRequiredService<GameActionService>();
                var abilityService = _serviceProvider.GetRequiredService<AbilityService>();
                var itemService = _serviceProvider.GetRequiredService<ItemService>();
                var mobService = _serviceProvider.GetRequiredService<MobService>();
                var mobSpawnService = _serviceProvider.GetRequiredService<MobSpawnService>();
                var gameMenuHandler = _serviceProvider.GetRequiredService<GameMenuHandler>();
                var battleHandler = _serviceProvider.GetRequiredService<BattleHandler>();
                var inventoryHandler = _serviceProvider.GetRequiredService<InventoryHandler>();
                var movementHandler = _serviceProvider.GetRequiredService<MovementHandler>();
                var specialActionsHandler = _serviceProvider.GetRequiredService<SpecialActionsHandler>();

                _callbackRouter = new CallbackRouter(
                    _botClient,
                    gameMenuHandler,
                    battleHandler,
                    inventoryHandler,
                    movementHandler,
                    specialActionsHandler
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации сервисов");
                throw;
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

                long chatId = 0;

                if (update.CallbackQuery != null)
                {
                    chatId = update.CallbackQuery.Message.Chat.Id;
                    if (_playerCreationUI.IsInCreation(chatId))
                    {
                        await _playerCreationUI.HandleCallback(chatId, update.CallbackQuery);
                    }
                    else
                    {
                        await _callbackRouter.HandleAsync(chatId, update.CallbackQuery.Data, update.CallbackQuery);
                    }
                    return;
                }

                if (update.Message is not { } message || message.Text is not { } messageText)
                    return;

                chatId = message.Chat.Id;

                if (messageText == "/start" || messageText.ToLower() == "меню")
                {
                    await _menuService.ShowMainMenu(chatId);
                    return;
                }

                if (_playerCreationUI.IsInCreation(chatId))
                {
                    await _playerCreationUI.HandleInput(chatId, messageText);
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
                    player = await _databaseService.GetPlayerSaveAsync(chatId);
                    if (player != null)
                    {
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

    }
}