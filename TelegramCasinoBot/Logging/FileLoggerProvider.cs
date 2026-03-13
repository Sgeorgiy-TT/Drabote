using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace TelegramMetroidvaniaBot.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly IOptionsMonitor<FileLoggerOptions> _options;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
        private readonly LogLevel _minLevel;
        private readonly bool _separateFilesByCategory;
        private readonly string _logDirectory;
        public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options)
        {
            _options = options;
            _minLevel = LogLevel.Information;
            _separateFilesByCategory = options.CurrentValue.SeparateFilesByCategory;
            _logDirectory = Path.GetDirectoryName(options.CurrentValue.Path);
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }
        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _options, _minLevel, _separateFilesByCategory, _logDirectory));
        }
        public void Dispose()
        {
            _loggers.Clear();
        }
    }
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly IOptionsMonitor<FileLoggerOptions> _options;
        private readonly LogLevel _minLevel;
        private readonly bool _separateFilesByCategory;
        private readonly string _logDirectory;
        private static readonly object _lock = new object();
        private static readonly ConcurrentDictionary<string, StreamWriter> _categoryWriters = new();
        public FileLogger(string categoryName, IOptionsMonitor<FileLoggerOptions> options, LogLevel minLevel, bool separateFilesByCategory, string logDirectory)
        {
            _categoryName = categoryName;
            _options = options;
            _minLevel = minLevel;
            _separateFilesByCategory = separateFilesByCategory;
            _logDirectory = logDirectory;
        }
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            var options = _options.CurrentValue;
            var logFilePath = GetLogFilePath(options.Path);
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {formatter(state, exception)}";
            if (exception != null)
            {
                logMessage += Environment.NewLine + $"Exception: {exception.Message}" + Environment.NewLine + $"StackTrace: {exception.StackTrace}";
            }
            lock (_lock)
            {
                try
                {
                    WriteToFile(logFilePath, logMessage);
                    if (_separateFilesByCategory)
                    {
                        var categoryFilePath = GetCategoryFilePath(_categoryName);
                        WriteToFile(categoryFilePath, logMessage);
                    }
                }
                catch
                {
                }
            }
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = GetColorForLogLevel(logLevel);
            Console.WriteLine(logMessage);
            Console.ForegroundColor = originalColor;
        }
        private void WriteToFile(string filePath, string message)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.AppendAllText(filePath, message + Environment.NewLine);
            var options = _options.CurrentValue;
            if (options.FileSizeLimitBytes > 0)
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists && fileInfo.Length > options.FileSizeLimitBytes)
                {
                    RotateLogFiles(filePath, options.MaxRollingFiles);
                }
            }
        }
        private string GetLogFilePath(string pathTemplate)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            return pathTemplate.Replace("-.", $"-{date}.");
        }
        private string GetCategoryFilePath(string categoryName)
        {
            var safeCategoryName = categoryName.Replace(".", "_").Replace("TelegramMetroidvaniaBot_", "");
            var categoryDir = Path.Combine(_logDirectory, "Categories");
            if (!Directory.Exists(categoryDir))
            {
                Directory.CreateDirectory(categoryDir);
            }
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            return Path.Combine(categoryDir, $"{safeCategoryName}_{date}.log");
        }
        private void RotateLogFiles(string logFilePath, int maxRollingFiles)
        {
            try
            {
                var directory = Path.GetDirectoryName(logFilePath);
                var fileName = Path.GetFileNameWithoutExtension(logFilePath);
                var extension = Path.GetExtension(logFilePath);
                for (int i = maxRollingFiles - 1; i >= 1; i--)
                {
                    var oldFile = Path.Combine(directory, $"{fileName}.{i}{extension}");
                    var newFile = Path.Combine(directory, $"{fileName}.{i + 1}{extension}");
                    if (File.Exists(oldFile))
                    {
                        if (i == maxRollingFiles - 1)
                        {
                            File.Delete(oldFile);
                        }
                        else
                        {
                            File.Move(oldFile, newFile, true);
                        }
                    }
                }
                var archiveFile = Path.Combine(directory, $"{fileName}.1{extension}");
                File.Move(logFilePath, archiveFile, true);
            }
            catch
            {
            }
        }
        private ConsoleColor GetColorForLogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Information => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }
    }
}
