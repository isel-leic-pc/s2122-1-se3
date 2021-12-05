using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace App
{
    public static class Logging
    {
        public static ILoggerFactory CreateFactory(params ILoggerProvider[] providers)
        {
            return LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("App", LogLevel.Debug)
                    .AddConsole();

                foreach (var provider in providers)
                {
                    builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton(provider));
                }
            });
        }
    }
}