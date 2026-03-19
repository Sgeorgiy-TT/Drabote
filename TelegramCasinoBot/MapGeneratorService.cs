using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace TelegramMetroidvaniaBot.Services
{
    public class MapGeneratorOptions
    {
        public int MaxImageDimension { get; set; } = 300; 
        public int JpegQuality { get; set; } = 50;
    }

    public class MapGeneratorService : IDisposable
    {
        private readonly ILogger<MapGeneratorService> _logger;
        private readonly MapGeneratorOptions _options;
        private readonly Rgba32 _gridColor = new(169, 169, 169, 150);
        private readonly Rgba32 _exploredColor = new(0, 255, 0, 60);
        private readonly Rgba32 _unexploredColor = new(0, 0, 0, 80);
        private readonly Rgba32 _chestColor = new(255, 215, 0, 200);
        private readonly Rgba32 _npcColor = new(0, 0, 255, 200);
        private readonly Rgba32 _enemyColor = new(255, 69, 0, 200);
        private readonly Rgba32 _obstacleColor = new(165, 42, 42, 200);
        private readonly Rgba32 _whiteColor = new(255, 255, 255, 255);

        private static readonly ConcurrentDictionary<string, Image<Rgba32>> _baseImageCache = new();
        private static readonly ConcurrentDictionary<string, Image<Rgba32>> _staticMapCache = new();
        private static readonly ConcurrentDictionary<string, Image<Rgba32>> _playerSpriteCache = new();
        private static Image<Rgba32> _cachedBarrierImage;
        private static readonly object _barrierLock = new();

        public MapGeneratorService(ILogger<MapGeneratorService> logger, IOptions<MapGeneratorOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task<Stream> GenerateLocationMap(
    string baseImagePath,
    int playerX,
    int playerY,
    int gridWidth,
    int gridHeight,
    List<Position> exploredAreas,
    Dictionary<string, List<Position>> locationObjects,
    List<LocationExit> exits,
    string playerSpritePath = null)
        {
            _logger.LogDebug("Начало GenerateLocationMap: путь {BaseImagePath}, игрок ({PlayerX},{PlayerY})", baseImagePath, playerX, playerY);
            var swTotal = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var baseImage = await GetCachedBaseImage(baseImagePath);
                sw.Stop();
                _logger.LogDebug("Загрузка базового изображения: {ElapsedMs} мс", sw.ElapsedMilliseconds);

                using var outputImage = baseImage.Clone();

                var cellWidth = outputImage.Width / gridWidth;
                var cellHeight = outputImage.Height / gridHeight;

                sw.Restart();
                var staticMap = await GetCachedStaticMap(baseImagePath, gridWidth, gridHeight, locationObjects, exits);
                sw.Stop();
                _logger.LogDebug("Получение статической карты: {ElapsedMs} мс", sw.ElapsedMilliseconds);

                sw.Restart();
                outputImage.Mutate(ctx => ctx.DrawImage(staticMap, 1f));
                sw.Stop();
                _logger.LogDebug("Наложение статической карты: {ElapsedMs} мс", sw.ElapsedMilliseconds);

                sw.Restart();
                outputImage.Mutate(ctx => DrawDynamicObjects(ctx, locationObjects, cellWidth, cellHeight));
                sw.Stop();
                _logger.LogDebug("Рисование динамических объектов: {ElapsedMs} мс", sw.ElapsedMilliseconds);

                sw.Restart();
                outputImage.Mutate(ctx =>
                    DrawExploredAreasOptimized(ctx, exploredAreas, gridWidth, gridHeight, cellWidth, cellHeight));
                sw.Stop();
                _logger.LogDebug("Затемнение исследованных областей: {ElapsedMs} мс", sw.ElapsedMilliseconds);

                sw.Restart();
                outputImage.Mutate(ctx =>
                    DrawPlayerWithSprite(ctx, playerX, playerY, cellWidth, cellHeight, playerSpritePath));
                sw.Stop();
                _logger.LogDebug("Рисование игрока: {ElapsedMs} мс", sw.ElapsedMilliseconds);

                sw.Restart();
                var resultStream = await SaveImageToStream(outputImage);
                sw.Stop();
                _logger.LogDebug("Сохранение в поток: {ElapsedMs} мс", sw.ElapsedMilliseconds);

                swTotal.Stop();
                _logger.LogInformation("Карта сгенерирована за {TotalMs} мс, размер: {Size} байт", swTotal.ElapsedMilliseconds, resultStream.Length);
                return resultStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации карты: {Message}", ex.Message);
                throw;
            }
            finally
            {
                _logger.LogDebug("GenerateLocationMap завершён");
            }
        }

        private async Task<Image<Rgba32>> GetCachedBaseImage(string imagePath)
        {
            if (_baseImageCache.TryGetValue(imagePath, out var cached))
            {
                _logger.LogDebug("Использован кэш базового изображения: {ImagePath}", imagePath);
                return cached.Clone();
            }

            _logger.LogInformation("Загрузка базового изображения: {ImagePath}", imagePath);
            var image = await Task.Run(() => Image.Load<Rgba32>(imagePath));

            if (image.Width > _options.MaxImageDimension || image.Height > _options.MaxImageDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(_options.MaxImageDimension, _options.MaxImageDimension),
                    Mode = ResizeMode.Max
                }));
            }

            _baseImageCache[imagePath] = image.Clone();
            return image;
        }

        private async Task<Image<Rgba32>> GetCachedStaticMap(
            string baseImagePath,
            int gridWidth,
            int gridHeight,
            Dictionary<string, List<Position>> locationObjects,
            List<LocationExit> exits)
        {
            var cacheKey = $"{baseImagePath}_{gridWidth}x{gridHeight}";
            if (_staticMapCache.TryGetValue(cacheKey, out var cached))
            {
                _logger.LogDebug("Использован кэш статической карты: {CacheKey}", cacheKey);
                return cached.Clone();
            }

            _logger.LogInformation("Генерация статической карты для {CacheKey}", cacheKey);
            var baseImage = await GetCachedBaseImage(baseImagePath);
            var staticImage = baseImage.CloneAs<Rgba32>();
            var cellWidth = staticImage.Width / gridWidth;
            var cellHeight = staticImage.Height / gridHeight;
            var barrierImage = GetCachedBarrierImage();

            staticImage.Mutate(ctx =>
            {
                DrawStaticBarriers(ctx, locationObjects, exits, gridWidth, gridHeight, cellWidth, cellHeight, barrierImage);
                DrawGridOptimized(ctx, gridWidth, gridHeight, cellWidth, cellHeight, staticImage.Width, staticImage.Height);
            });

            _staticMapCache[cacheKey] = staticImage.Clone();
            return staticImage;
        }

        private Image<Rgba32> GetCachedBarrierImage()
        {
            lock (_barrierLock)
            {
                if (_cachedBarrierImage != null)
                {
                    _logger.LogDebug("Использован кэш изображения барьера");
                    return _cachedBarrierImage.Clone();
                }

                var barrierPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "barer.jpg");
                if (File.Exists(barrierPath))
                {
                    _logger.LogInformation("Загрузка изображения барьера");
                    _cachedBarrierImage = Image.Load<Rgba32>(barrierPath);
                    return _cachedBarrierImage.Clone();
                }
                _logger.LogWarning("Файл барьера не найден, будет использована заливка цветом");
                return null;
            }
        }

        private void DrawStaticBarriers(IImageProcessingContext ctx,
            Dictionary<string, List<Position>> objects, List<LocationExit> exits,
            int gridWidth, int gridHeight, int cellWidth, int cellHeight, Image barrierImage)
        {
            if (objects == null || !objects.ContainsKey("obstacles"))
            {
                _logger.LogDebug("Нет препятствий для отрисовки");
                return;
            }

            var exitPositions = new HashSet<(int, int)>();
            if (exits != null)
            {
                foreach (var exit in exits)
                    exitPositions.Add((exit.Position.X, exit.Position.Y));
            }

            int barrierCount = 0;
            foreach (var pos in objects["obstacles"])
            {
                if (exitPositions.Contains((pos.X, pos.Y))) continue;

                var centerX = pos.X * cellWidth + cellWidth / 2;
                var centerY = pos.Y * cellHeight + cellHeight / 2;
                var barrierSize = Math.Min(cellWidth, cellHeight) * 0.75f;

                DrawBarrierImage(ctx, barrierImage, centerX, centerY, barrierSize);
                barrierCount++;
            }
            _logger.LogDebug("Отрисовано барьеров: {BarrierCount}", barrierCount);
        }

        private void DrawBarrierImage(IImageProcessingContext ctx, Image barrierImage, float centerX, float centerY, float size)
        {
            try
            {
                using var resized = barrierImage.Clone(x => x.Resize(new ResizeOptions
                {
                    Size = new Size((int)size, (int)size),
                    Mode = ResizeMode.Stretch
                }));
                var x = (int)(centerX - size / 2);
                var y = (int)(centerY - size / 2);
                ctx.DrawImage(resized, new Point(x, y), 0.8f);
            }
            catch
            {
                var color = GetObjectColor("obstacles");
                var rect = new Rectangle((int)(centerX - size / 2), (int)(centerY - size / 2), (int)size, (int)size);
                ctx.Fill(color, rect);
            }
        }

        private void DrawGridOptimized(IImageProcessingContext ctx, int gridWidth, int gridHeight,
            int cellWidth, int cellHeight, int imageWidth, int imageHeight)
        {
            for (int x = 0; x <= gridWidth; x++)
            {
                var lineX = x * cellWidth;
                ctx.DrawLines(_gridColor, 1f, new PointF(lineX, 0), new PointF(lineX, imageHeight));
            }
            for (int y = 0; y <= gridHeight; y++)
            {
                var lineY = y * cellHeight;
                ctx.DrawLines(_gridColor, 1f, new PointF(0, lineY), new PointF(imageWidth, lineY));
            }
            _logger.LogDebug("Сетка отрисована");
        }

        private void DrawDynamicObjects(IImageProcessingContext ctx,
            Dictionary<string, List<Position>> objects,
            int cellWidth, int cellHeight)
        {
            if (objects == null)
            {
                _logger.LogDebug("Нет динамических объектов");
                return;
            }

            int objectCount = 0;
            foreach (var objType in objects.Where(o => o.Key != "obstacles"))
            {
                foreach (var pos in objType.Value)
                {
                    var centerX = pos.X * cellWidth + cellWidth / 2;
                    var centerY = pos.Y * cellHeight + cellHeight / 2;
                    var markerSize = Math.Min(cellWidth, cellHeight) / 3;

                    var color = GetObjectColor(objType.Key);
                    var rect = new Rectangle(
                        (int)(centerX - markerSize / 2),
                        (int)(centerY - markerSize / 2),
                        (int)markerSize,
                        (int)markerSize
                    );

                    ctx.Fill(color, rect);
                    ctx.Draw(_whiteColor, 1f, rect);
                    objectCount++;
                }
            }
            _logger.LogDebug("Отрисовано динамических объектов: {ObjectCount}", objectCount);
        }

        private void DrawExploredAreasOptimized(IImageProcessingContext ctx, List<Position> exploredAreas,
            int gridWidth, int gridHeight, int cellWidth, int cellHeight)
        {
            if (exploredAreas == null)
            {
                _logger.LogDebug("Нет исследованных областей");
                return;
            }

            foreach (var area in exploredAreas)
            {
                var rect = new Rectangle(area.X * cellWidth, area.Y * cellHeight, cellWidth, cellHeight);
                ctx.Fill(_exploredColor, rect);
            }

            if (exploredAreas.Count >= gridWidth * gridHeight)
            {
                _logger.LogDebug("Вся карта исследована");
                return;
            }

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (!exploredAreas.Any(p => p.X == x && p.Y == y))
                    {
                        var rect = new Rectangle(x * cellWidth, y * cellHeight, cellWidth, cellHeight);
                        ctx.Fill(_unexploredColor, rect);
                    }
                }
            }
            _logger.LogDebug("Исследовано клеток: {ExploredCount} из {TotalCells}", exploredAreas.Count, gridWidth * gridHeight);
        }

        private void DrawPlayerWithSprite(IImageProcessingContext ctx, int playerX, int playerY,
            int cellWidth, int cellHeight, string playerSpritePath)
        {
            var centerX = playerX * cellWidth + cellWidth / 2;
            var centerY = playerY * cellHeight + cellHeight / 2;
            var size = Math.Min(cellWidth, cellHeight) / 2;

            if (!string.IsNullOrEmpty(playerSpritePath) && File.Exists(playerSpritePath))
            {
                try
                {
                    if (!_playerSpriteCache.TryGetValue(playerSpritePath, out var sprite))
                    {
                        _logger.LogInformation("Загрузка спрайта игрока: {PlayerSpritePath}", playerSpritePath);
                        sprite = Image.Load<Rgba32>(playerSpritePath);
                        _playerSpriteCache[playerSpritePath] = sprite;
                    }

                    using var resizedSprite = sprite.Clone(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size((int)size, (int)size),
                        Mode = ResizeMode.Stretch
                    }));

                    var x = (int)(centerX - size / 2);
                    var y = (int)(centerY - size / 2);
                    ctx.DrawImage(resizedSprite, new Point(x, y), 1f);
                    _logger.LogDebug("Спрайт игрока отрисован");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось загрузить спрайт игрока {PlayerSpritePath}: {Message}", playerSpritePath, ex.Message);
                }
            }

            var points = new PointF[]
            {
                new(centerX, centerY - size / 2),
                new(centerX - size / 2, centerY + size / 2),
                new(centerX + size / 2, centerY + size / 2)
            };
            ctx.FillPolygon(new Rgba32(255, 0, 0, 200), points);
            ctx.DrawPolygon(_whiteColor, 2f, points);
            _logger.LogDebug("Игрок отрисован треугольником (спрайт отсутствует)");
        }

        private Rgba32 GetObjectColor(string objectType) => objectType.ToLower() switch
        {
            "chests" => _chestColor,
            "npcs" => _npcColor,
            "enemies" => _enemyColor,
            "obstacles" => _obstacleColor,
            _ => new Rgba32(128, 128, 128, 200)
        };

        private async Task<Stream> SaveImageToStream(Image<Rgba32> image)
        {
            var ms = new MemoryStream();
            var encoder = new JpegEncoder { Quality = _options.JpegQuality };
            await Task.Run(() => image.SaveAsJpeg(ms, encoder));
            ms.Position = 0;
            _logger.LogDebug("Изображение сохранено в поток JPEG (качество {JpegQuality}), размер: {Size} байт", _options.JpegQuality, ms.Length);
            return ms;
        }

        public void ClearCache()
        {
            _logger.LogInformation("Начало ClearCache");
            foreach (var img in _baseImageCache.Values) img?.Dispose();
            _baseImageCache.Clear();
            foreach (var img in _staticMapCache.Values) img?.Dispose();
            _staticMapCache.Clear();
            foreach (var img in _playerSpriteCache.Values) img?.Dispose();
            _playerSpriteCache.Clear();
            _cachedBarrierImage?.Dispose();
            _cachedBarrierImage = null;
            _logger.LogInformation("Кэш изображений очищен");
        }

        public void Dispose()
        {
            ClearCache();
        }
    }
}