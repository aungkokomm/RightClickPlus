using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using RightClick.Core.Services;
using RightClick.Infrastructure;
using RightClick_App.ViewModels;

namespace RightClick_App;

/// <summary>
/// Application entry point. Configures dependency injection, logging, and loads
/// settings before showing the main window.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    /// <summary>The app's DI container.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>The current application instance.</summary>
    public new static App Current => (App)Application.Current;

    /// <summary>The main application window.</summary>
    public Window? MainWindow => _window;

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var settings = Services.GetRequiredService<ISettingsService>();
        await settings.LoadAsync();

        var cmdline = Environment.GetCommandLineArgs();

        // Launched as the note editor:  RightClick.App.exe note "<path>"
        if (cmdline.Length >= 3 && string.Equals(cmdline[1], "note", StringComparison.OrdinalIgnoreCase))
        {
            _window = new NoteEditorWindow(cmdline[2]);
            _window.Activate();
            return;
        }

        RefreshShellVerbs();

        _window = new MainWindow();
        _window.Activate();
    }

    /// <summary>
    /// Reconciles the registered right-click verbs to the SAVED settings on every launch: each feature
    /// that is enabled in settings is (re-)registered fresh (so it's always present and matches this
    /// build's menu/icons/paths), and each disabled feature is removed. This is the source of truth —
    /// previously we only refreshed verbs already in the registry, which meant that if a verb went
    /// missing (e.g. after a reinstall, which wipes the entries) the app could never restore it even
    /// though the toggle still showed On, leaving a "Folder color" menu that wasn't actually there.
    /// </summary>
    private static void RefreshShellVerbs()
    {
        try
        {
            var shell = Services.GetRequiredService<IShellVerbService>();
            var settings = Services.GetRequiredService<ISettingsService>().Current;
            foreach (RightClick.Core.Models.ShellAction action in Enum.GetValues<RightClick.Core.Models.ShellAction>())
            {
                try
                {
                    if (IsEnabledInSettings(settings, action))
                    {
                        // Clean re-write: remove any stale/old registration, then register fresh.
                        shell.Uninstall(action);
                        shell.Install(action);
                    }
                    else if (shell.IsInstalled(action))
                    {
                        shell.Uninstall(action);
                    }
                }
                catch
                {
                    // One feature's registry hiccup shouldn't stop the others.
                }
            }

            // Tell the shell associations changed and flush the icon cache, so updated menu
            // icons (e.g. new folder-colour art) replace any version Explorer cached by path.
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
        }
        catch
        {
            // A registry hiccup shouldn't stop the app from opening.
        }
    }

    private static bool IsEnabledInSettings(RightClick.Core.Models.AppSettings s, RightClick.Core.Models.ShellAction action) => action switch
    {
        RightClick.Core.Models.ShellAction.FileToFolder => s.FileToFolderEnabled,
        RightClick.Core.Models.ShellAction.FileNotes => s.FileNotesEnabled,
        RightClick.Core.Models.ShellAction.CopyFileNames => s.CopyFileNamesEnabled,
        RightClick.Core.Models.ShellAction.FolderColor => s.FolderColorEnabled,
        _ => false,
    };

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;
    private const uint SHCNF_FLUSH = 0x1000;

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddDebug();
        });

        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IContextMenuTweakService, ContextMenuTweakService>();
        services.AddSingleton<IShellVerbService, ShellVerbService>();

        // ViewModels
        services.AddTransient<MenuModeViewModel>();
        services.AddTransient<ActionsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<NoteEditorViewModel>();

        return services.BuildServiceProvider();
    }
}
