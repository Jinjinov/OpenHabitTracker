using Microsoft.Extensions.Logging;

namespace OpenHabitTracker.App;

// Warning and above only: Error.log is for what went wrong, and the informational lines already go
// to the debug and console providers every host registers.
public sealed class FileLoggerProvider(string directory, LogLevel minimumLevel = LogLevel.Warning) : ILoggerProvider
{
    private readonly string _directory = directory;
    private readonly LogLevel _minimumLevel = minimumLevel;

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_directory, categoryName, _minimumLevel);
    }

    public void Dispose()
    {
    }

    private sealed class FileLogger(string directory, string category, LogLevel minimumLevel) : ILogger
    {
        private readonly string _directory = directory;
        private readonly string _category = category;
        private readonly LogLevel _minimumLevel = minimumLevel;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= _minimumLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string message;

            try
            {
                message = formatter(state, exception);
            }
            catch
            {
                message = "(the message could not be formatted)";
            }

            FileLog.Write(_directory, logLevel, _category, message, exception);
        }
    }
}
