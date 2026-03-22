using System.Collections.Generic;

namespace TelegramCasinoBot.Utils
{
    public class ImageSettings
    {
        public Dictionary<string, ImageCategorySettings> Categories { get; set; }
    }

    public class ImageCategorySettings
    {
        public int MaxDimension { get; set; } = 800;
        public int JpegQuality { get; set; } = 80;
        public bool EnableCache { get; set; } = true;
    }
}