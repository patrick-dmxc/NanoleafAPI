using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NanoleafAPI_Tests
{
    internal class TestLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName);
        }

        public void Dispose()
        {
        }
    }
    internal class TestLogger : Microsoft.Extensions.Logging.ILogger
    {
        public TestLogger(string categoryName)
        {

        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Console.WriteLine(formatter.Invoke(state, exception));
            System.Diagnostics.Debug.WriteLine(formatter.Invoke(state, exception));
        }
    }
}