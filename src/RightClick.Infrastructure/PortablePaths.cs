namespace RightClick.Infrastructure;

/// <summary>
/// Resolves where the app stores its data. Portable by design: if a writable
/// <c>Data</c> folder can live next to the executable (or a <c>portable.flag</c>
/// marker is present), state is kept there; otherwise it falls back to
/// <c>%LOCALAPPDATA%\RightClickPlus</c> for installed (Program Files) deployments.
/// </summary>
public static class PortablePaths
{
    public const string AppFolderName = "RightClickPlus";

    private static readonly Lazy<string> _dataRoot = new(ResolveDataRoot);

    /// <summary>Root folder for all app-written state. Guaranteed to exist.</summary>
    public static string DataRoot => _dataRoot.Value;

    /// <summary>Full path to the settings JSON file.</summary>
    public static string SettingsFile => Path.Combine(DataRoot, "settings.json");

    /// <summary>Folder for log files.</summary>
    public static string LogsFolder => EnsureDir(Path.Combine(DataRoot, "logs"));

    private static string ResolveDataRoot()
    {
        var exeDir = AppContext.BaseDirectory;
        var portableMarker = Path.Combine(exeDir, "portable.flag");
        var localData = Path.Combine(exeDir, "Data");

        // Explicit portable mode, or a location we can actually write to.
        if (File.Exists(portableMarker) || IsWritable(exeDir))
        {
            return EnsureDir(localData);
        }

        var roaming = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName);
        return EnsureDir(roaming);
    }

    private static bool IsWritable(string dir)
    {
        try
        {
            var probe = Path.Combine(dir, $".write-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string EnsureDir(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
