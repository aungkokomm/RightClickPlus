namespace RightClick.Infrastructure;

/// <summary>
/// Resolves where the app stores its data. Portable by design: if a writable
/// <c>Data</c> folder can live next to the executable (or a <c>portable.flag</c>
/// marker is present), state is kept there; otherwise, and always for installs under
/// Program Files, it uses <c>%LOCALAPPDATA%\RightClickPlus</c>.
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

        // Explicit portable mode, or a portable copy in a folder we can write to. An install under
        // Program Files is never treated as portable: it is only writable when running as administrator,
        // so picking the folder by writability would split the settings between elevated and normal runs.
        if (File.Exists(portableMarker) || (!IsUnderProgramFiles(exeDir) && IsWritable(exeDir)))
        {
            return EnsureDir(localData);
        }

        var userData = EnsureDir(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName));
        AdoptNewerSettings(Path.Combine(localData, "settings.json"), Path.Combine(userData, "settings.json"));
        return userData;
    }

    private static bool IsUnderProgramFiles(string dir) =>
        new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 }
            .Select(Environment.GetFolderPath)
            .Where(p => !string.IsNullOrEmpty(p))
            .Any(p => dir.StartsWith(p.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Versions up to 1.0.6 kept settings next to the exe when run as administrator. Copy that file
    /// over when it is newer, so no choices are lost. The old file itself is left untouched.
    /// </summary>
    private static void AdoptNewerSettings(string oldFile, string newFile)
    {
        try
        {
            if (File.Exists(oldFile) &&
                (!File.Exists(newFile) || File.GetLastWriteTimeUtc(oldFile) > File.GetLastWriteTimeUtc(newFile)))
            {
                File.Copy(oldFile, newFile, overwrite: true); // keeps the old timestamp, so this runs once
            }
        }
        catch
        {
            // Best effort: the current settings file (or defaults) is used if the copy fails.
        }
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
