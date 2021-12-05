using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Tests
{
    public class XUnitLoggingProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _outputHelper;

        public XUnitLoggingProvider(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public void Dispose()
        {
            // nothing
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_outputHelper);
        }
        
        
        
        private class XUnitLogger : ILogger
        {
            private readonly ITestOutputHelper _outputHelper;

            public XUnitLogger(ITestOutputHelper outputHelper)
            {
                _outputHelper = outputHelper;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                _outputHelper.WriteLine(formatter(state, exception));
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }
        }
    }
}