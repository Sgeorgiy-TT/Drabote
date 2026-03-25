using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using TelegramCasinoBot.Utils;

namespace TelegramCasinoBot.Services.Infrastructure
{
    public class ImageService
    {
        public static readonly string DefaultCategory = "Default";
        public static readonly string MenuCategory = "Menu";
        public static readonly string LocationMapCategory = "LocationMap";
        public static readonly string CharacterIconCategory = "CharacterIcon";

        private readonly ILogger<ImageService> _logger;
        private readonly IOptions<ImageSettings> _settings;
        private readonly ConcurrentDictionary<string, byte[]> _cache = new();

        public ImageService(ILogger<ImageService> logger, IOptions<ImageSettings> settings)
        {
            _logger = logger;
            _settings = settings;
        }
        public ImageCategorySettings GetCategorySettings(string category)
        {
            if (_settings.Value.Categories != null &&
                _settings.Value.Categories.TryGetValue(category, out var settings))
            {
                return settings;
            }
            return new ImageCategorySettings();
        }

        public async ValueTask<Stream> GetProcessedImageAsync(string fileName, string? category = null, bool? enableCache = null, CancellationToken cancellationToken = default)
        {
            category ??= DefaultCategory;
            var categorySettings = GetCategorySettings(category);
            return await GetProcessedImageAsync(fileName, category, categorySettings.JpegQuality, enableCache ?? categorySettings.EnableCache, cancellationToken);
        }

        public async ValueTask<Stream> GetProcessedImageAsync(string fileName, string category, int quality, bool? enableCache = null, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(_settings.Value.BaseImagePath, fileName);
            var categorySettings = GetCategorySettings(category);
            return await ProcessImageAsync(fullPath, categorySettings.MaxDimension, quality, enableCache ?? categorySettings.EnableCache, cancellationToken);
        }
        //fullPath перенести сюда
        private async ValueTask<Stream> ProcessImageAsync(string imagePath, int maxDimension, int jpegQuality, bool enableCache, CancellationToken cancellationToken)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Изображение не найдено: {imagePath}");

            var cacheKey = $"{imagePath}_{maxDimension}_{jpegQuality}";

            if (enableCache && _cache.TryGetValue(cacheKey, out var cachedBytes))
            {
                _logger.LogDebug("Возвращаем кэшированное изображение для {ImagePath}", imagePath);
                return new MemoryStream(cachedBytes);
            }

            _logger.LogDebug("Обрабатываем изображение {ImagePath}", imagePath);

            using var image = await Task.Run(() => Image.Load(imagePath), cancellationToken);
            using var output = new MemoryStream();

            if (image.Width > maxDimension || image.Height > maxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(maxDimension, maxDimension),
                    Mode = ResizeMode.Max
                }));
            }

            var encoder = new JpegEncoder { Quality = jpegQuality };
            await Task.Run(() => image.Save(output, encoder), cancellationToken);

            var bytes = output.ToArray();
            if (enableCache)
                _cache[cacheKey] = bytes;

            var resultStream = new MemoryStream(bytes);
            resultStream.Position = 0;
            return resultStream;
        }

        
    }
}