using System;
using System.IO;
using System.Text;
using System.Threading;
namespace TelegramMetroidvaniaBot.Services
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
    public sealed class LoggerService : IDisposable
    {
        private static readonly Lazy<LoggerService> _instance = new Lazy<LoggerService>(() => new LoggerService());
        private readonly string _logDirectory;
        private readonly LogLevel _minLogLevel;
        private readonly object _lock = new object();
        private StreamWriter _currentWriter;
        private string _currentDate;
        private bool _disposed;
        public static LoggerService Instance => _instance.Value;
        private LoggerService()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "appsettings.json");
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            }
            _logDirectory = "Logs";
            _minLogLevel = LogLevel.Info;
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    if (json.Contains("\"Logging\""))
                    {
                        var start = json.IndexOf("\"Logging\"");
                        var braceStart = json.IndexOf("{", start);
                        var braceEnd = json.IndexOf("}", braceStart);
                        var loggingSection = json.Substring(braceStart, braceEnd - braceStart + 1);
                        if (loggingSection.Contains("LogLevel"))
                        {
                            var levelStart = loggingSection.IndexOf("LogLevel");
                            var colon = loggingSection.IndexOf(":", levelStart);
                            var comma = loggingSection.IndexOf(",", levelStart);
                            var end = comma > 0 ? comma : loggingSection.Length;
                            var levelStr = json.Substring(colon + 1, end - colon - 1).Trim().Trim('"', ' ', '}');
                            if (Enum.TryParse<LogLevel>(levelStr, true, out var parsedLevel))
                            {
                                _minLogLevel = parsedLevel;
                            }
                        }
                        if (loggingSection.Contains("LogDirectory"))
                        {
                            var dirStart = loggingSection.IndexOf("LogDirectory");
                            var colon = loggingSection.IndexOf(":", dirStart);
                            var comma = loggingSection.IndexOf(",", dirStart);
                            var end = comma > 0 ? comma : loggingSection.Length;
                            var dirStr = json.Substring(colon + 1, end - colon - 1).Trim().Trim('"', ' ', '}');
                            if (!string.IsNullOrEmpty(dirStr))
                            {
                                _logDirectory = dirStr;
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
            _currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            InitializeWriter();
        }
        private void InitializeWriter()
        {
            string fileName = $"log_{_currentDate}.txt";
            string filePath = Path.Combine(_logDirectory, fileName);
            _currentWriter = new StreamWriter(filePath, true, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }
        private void EnsureWriterIsCurrent()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (today != _currentDate)
            {
                lock (_lock)
                {
                    if (today != _currentDate)
                    {
                        _currentDate = today;
                        _currentWriter?.Dispose();
                        InitializeWriter();
                    }
                }
            }
        }
        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }
        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }
        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }
        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }
        public void Error(string message, Exception exception)
        {
            Log(LogLevel.Error, $"{message}\nException: {exception.Message}\nStackTrace: {exception.StackTrace}");
        }
        private void Log(LogLevel level, string message)
        {
            if (level < _minLogLevel)
            {
                return;
            }
            EnsureWriterIsCurrent();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();
            string logMessage = $"[{timestamp}] [{levelStr}] {message}";
            lock (_lock)
            {
                _currentWriter?.WriteLine(logMessage);
            }
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = GetColorForLevel(level);
            Console.WriteLine(logMessage);
            Console.ForegroundColor = originalColor;
        }
        private ConsoleColor GetColorForLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
        }
        public void Dispose()
        {
            if (!_disposed)
            {
                _currentWriter?.Dispose();
                _disposed = true;
            }
        }
    }
}
