namespace TelegramMetroidvaniaBot.Logging
{
    public class FileLoggerOptions
    {
        public string Path { get; set; } = "Logs/log-.txt";
        public bool Append { get; set; } = true;
        public long FileSizeLimitBytes { get; set; } = 10485760; // 10 MB
        public int MaxRollingFiles { get; set; } = 7;
        public bool SeparateFilesByCategory { get; set; } = false; // Записывать ли логи в разные файлы по категориям
    }
}
