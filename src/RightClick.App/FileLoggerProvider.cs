using Microsoft.Extensions.Logging;
using RightClick.Infrastructure;

namespace RightClick_App;

/// <summary>
/// Appends warnings and errors to <c>logs\app.log</c> in the app's data folder, so failures (e.g. a
/// right-click entry that couldn't be registered) can be diagnosed in release builds.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private static readonly object Gate = new();

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class FileLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {logLevel.ToString().ToUpperInvariant()} {category}: {formatter(state, exception)}" +
                           (exception is null ? string.Empty : $" :: {exception}");
                lock (Gate)
                {
                    File.AppendAllText(Path.Combine(PortablePaths.LogsFolder, "app.log"), line + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never throw.
            }
        }
    }
}
