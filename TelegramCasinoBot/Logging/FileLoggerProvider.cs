using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TelegramMetroidvaniaBot.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly IOptionsMonitor<FileLoggerOptions> _options;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

        public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> options)
        {
            _options = options;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _options));
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
        private static readonly object _lock = new object();

        public FileLogger(string categoryName, IOptionsMonitor<FileLoggerOptions> options)
        {
            _categoryName = categoryName;
            _options = options;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

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
                    var directory = Path.GetDirectoryName(logFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);

                    // Проверка размера файла и ротация
                    if (options.FileSizeLimitBytes > 0)
                    {
                        var fileInfo = new FileInfo(logFilePath);
                        if (fileInfo.Exists && fileInfo.Length > options.FileSizeLimitBytes)
                        {
                            RotateLogFiles(logFilePath, options.MaxRollingFiles);
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибки записи в лог
                }
            }

            // Вывод в консоль с цветом
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = GetColorForLogLevel(logLevel);
            Console.WriteLine(logMessage);
            Console.ForegroundColor = originalColor;
        }

        private string GetLogFilePath(string pathTemplate)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            return pathTemplate.Replace("-.", $"-{date}.");
        }

        private void RotateLogFiles(string logFilePath, int maxRollingFiles)
        {
            try
            {
                var directory = Path.GetDirectoryName(logFilePath);
                var fileName = Path.GetFileNameWithoutExtension(logFilePath);
                var extension = Path.GetExtension(logFilePath);

                // Удаляем самый старый файл, если достигнут лимит
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

                // Переименовываем текущий файл
                var archiveFile = Path.Combine(directory, $"{fileName}.1{extension}");
                File.Move(logFilePath, archiveFile, true);
            }
            catch
            {
                // Игнорируем ошибки ротации
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
