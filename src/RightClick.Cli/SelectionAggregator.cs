namespace RightClick.Cli;

/// <summary>
/// Coordinates the per-file processes the shell launches for a multi-file selection.
/// Classic registry verbs invoke the command once per selected file; these processes
/// rendezvous through a temp session file (guarded by a named mutex). The first process
/// in a batch becomes the "leader": it waits briefly for the rest to register, then acts
/// on the full set. Followers simply exit.
/// </summary>
internal static class SelectionAggregator
{
    private static readonly TimeSpan Window = TimeSpan.FromMilliseconds(800);

    /// <summary>
    /// Registers <paramref name="path"/> in the named batch. Returns true if this process is the
    /// leader. When leader, <paramref name="collect"/> (called after the debounce window) yields the
    /// complete set of paths in the batch.
    /// </summary>
    public static bool TryBecomeLeader(string sessionName, string path, out Func<IReadOnlyList<string>> collect)
    {
        var sessionFile = Path.Combine(Path.GetTempPath(), $"rightclickplus.{Sanitize(sessionName)}.session");
        var mutex = new Mutex(initiallyOwned: false, $"Global\\RightClickPlus.{Sanitize(sessionName)}");

        bool isLeader;
        mutex.WaitOne();
        try
        {
            var fresh = !File.Exists(sessionFile) ||
                        (DateTime.UtcNow - File.GetLastWriteTimeUtc(sessionFile)) > Window;
            if (fresh)
            {
                File.WriteAllText(sessionFile, string.Empty);
                isLeader = true;
            }
            else
            {
                isLeader = false;
            }

            File.AppendAllText(sessionFile, path + Environment.NewLine);
        }
        finally
        {
            mutex.ReleaseMutex();
        }

        if (!isLeader)
        {
            collect = () => Array.Empty<string>();
            mutex.Dispose();
            return false;
        }

        collect = () =>
        {
            // Give the rest of the batch time to register, then take the whole set.
            Thread.Sleep(Window);
            mutex.WaitOne();
            try
            {
                var lines = File.Exists(sessionFile)
                    ? File.ReadAllLines(sessionFile)
                    : Array.Empty<string>();
                try { File.Delete(sessionFile); } catch { /* best effort */ }
                return lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            }
            finally
            {
                mutex.ReleaseMutex();
                mutex.Dispose();
            }
        };
        return true;
    }

    private static string Sanitize(string s) =>
        string.Concat(s.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.'));
}
