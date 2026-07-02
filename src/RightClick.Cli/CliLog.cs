using RightClick.Infrastructure;

namespace RightClick.Cli;

/// <summary>Minimal append-only logger for the helper exe (it has no console).</summary>
internal static class CliLog
{
    private static readonly object Gate = new();

    public static void Error(string message, Exception? ex = null)
    {
        try
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR {message}" +
                       (ex is null ? string.Empty : $" :: {ex}");
            lock (Gate)
            {
                File.AppendAllText(Path.Combine(PortablePaths.LogsFolder, "cli.log"), line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never throw from the helper.
        }
    }
}
