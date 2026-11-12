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
    /// Reconciles the registered right-click verbs to the saved settings on every launch (see
    /// <see cref="ShellVerbReconciler"/>). Failures are written to Data\logs\app.log.
    /// </summary>
    private static void RefreshShellVerbs()
    {
        var logger = Services.GetRequiredService<ILogger<App>>();
        try
        {
            ShellVerbReconciler.Reconcile(
                Services.GetRequiredService<IShellVerbService>(),
                Services.GetRequiredService<ISettingsService>().Current,
                (message, ex) => logger.LogError(ex, "{Message}", message));
        }
        catch (Exception ex)
        {
            // A registry hiccup shouldn't stop the app from opening.
            logger.LogError(ex, "Refreshing the right-click entries failed.");
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddDebug();
            builder.AddProvider(new FileLoggerProvider());
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
