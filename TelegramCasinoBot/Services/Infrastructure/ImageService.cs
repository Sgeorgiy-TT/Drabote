using System;
using System.Collections.Concurrent;
using System.IO;
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
        public const string DefaultCategory = "Default";
        public const string MenuCategory = "Menu";
        public const string LocationMapCategory = "LocationMap";
        public const string CharacterIconCategory = "CharacterIcon";

        private readonly ILogger<ImageService> _logger;
        private readonly IOptions<ImageSettings> _settings;
        private readonly ConcurrentDictionary<string, byte[]> _cache = new();

        public ImageService(ILogger<ImageService> logger, IOptions<ImageSettings> settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public async Task<Stream> GetProcessedImageAsync(string fileName, string category = DefaultCategory)
        {
            var fullPath = Path.Combine(_settings.Value.BaseImagePath, fileName);
            return await GetProcessedImageFromPathAsync(fullPath, category);
        }

        public async Task<Stream> GetProcessedImageWithQualityAsync(string fileName, string category, int quality)
        {
            var fullPath = Path.Combine(_settings.Value.BaseImagePath, fileName);
            return await GetProcessedImageFromPathWithQualityAsync(fullPath, category, quality);
        }

        public async Task<Stream> GetProcessedImageFromFullPathAsync(string fullPath, string category = DefaultCategory)
        {
            return await GetProcessedImageFromPathAsync(fullPath, category);
        }

        private async Task<Stream> GetProcessedImageFromPathAsync(string imagePath, string category)
        {
            var categorySettings = GetCategorySettings(category);
            return await ProcessImage(imagePath, category, categorySettings.MaxDimension, categorySettings.JpegQuality, categorySettings.EnableCache);
        }

        private async Task<Stream> GetProcessedImageFromPathWithQualityAsync(string imagePath, string category, int quality)
        {
            var categorySettings = GetCategorySettings(category);
            return await ProcessImage(imagePath, category, categorySettings.MaxDimension, quality, categorySettings.EnableCache);
        }

        private async Task<Stream> ProcessImage(string imagePath, string category, int maxDimension, int jpegQuality, bool enableCache)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Изображение не найдено: {imagePath}");

            var cacheKey = $"{imagePath}_{category}_{maxDimension}_{jpegQuality}";

            if (enableCache && _cache.TryGetValue(cacheKey, out var cachedBytes))
            {
                _logger.LogDebug("Возвращаем кэшированное изображение для {ImagePath} (категория {Category})", imagePath, category);
                var ms = new MemoryStream(cachedBytes);
                ms.Position = 0;
                return ms;
            }

            _logger.LogDebug("Обрабатываем изображение {ImagePath} (категория {Category})", imagePath, category);

            using var image = await Task.Run(() => Image.Load(imagePath));
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
            await Task.Run(() => image.Save(output, encoder));

            var bytes = output.ToArray();
            if (enableCache)
                _cache[cacheKey] = bytes;

            var resultStream = new MemoryStream(bytes);
            resultStream.Position = 0;
            return resultStream;
        }

        private ImageCategorySettings GetCategorySettings(string category)
        {
            if (_settings.Value.Categories != null &&
                _settings.Value.Categories.TryGetValue(category, out var settings))
            {
                return settings;
            }
            return new ImageCategorySettings(); // fallback
        }
    }
}