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
        private readonly ILogger<ImageService> _logger;
        private readonly IOptions<ImageSettings> _settings;
        private readonly ConcurrentDictionary<string, byte[]> _cache = new();

        public ImageService(ILogger<ImageService> logger, IOptions<ImageSettings> settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public async Task<Stream> GetProcessedImageAsync(string imagePath, string category = "Default")
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException($"Изображение не найдено: {imagePath}");

            var categorySettings = GetCategorySettings(category);
            var cacheKey = $"{imagePath}_{category}_{categorySettings.MaxDimension}_{categorySettings.JpegQuality}";

            if (categorySettings.EnableCache && _cache.TryGetValue(cacheKey, out var cachedBytes))
            {
                _logger.LogDebug("Возвращаем кэшированное изображение для {ImagePath} (категория {Category})", imagePath, category);
                var ms = new MemoryStream(cachedBytes);
                ms.Position = 0;
                return ms;
            }

            _logger.LogDebug("Обрабатываем изображение {ImagePath} (категория {Category})", imagePath, category);

            using var image = await Task.Run(() => Image.Load(imagePath));
            using var output = new MemoryStream();

            if (image.Width > categorySettings.MaxDimension || image.Height > categorySettings.MaxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(categorySettings.MaxDimension, categorySettings.MaxDimension),
                    Mode = ResizeMode.Max
                }));
            }

            var encoder = new JpegEncoder { Quality = categorySettings.JpegQuality };
            await Task.Run(() => image.Save(output, encoder));

            var bytes = output.ToArray();
            if (categorySettings.EnableCache)
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
            return new ImageCategorySettings();
        }
    }
}