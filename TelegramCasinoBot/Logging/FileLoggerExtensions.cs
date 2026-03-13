using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
namespace TelegramMetroidvaniaBot.Logging
{
    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions
                <FileLoggerOptions, FileLoggerProvider>(builder.Services);
            return builder;
        }
    }
}
