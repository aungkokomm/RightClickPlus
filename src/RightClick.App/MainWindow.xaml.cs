using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using RightClick.Core.Models;
using RightClick.Core.Services;
using RightClick_App.ViewModels;
using Windows.Graphics;

namespace RightClick_App;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    public MenuModeViewModel Menu { get; }
    public ActionsViewModel Actions { get; }

    public MainWindow()
    {
        Menu = App.Services.GetRequiredService<MenuModeViewModel>();
        Actions = App.Services.GetRequiredService<ActionsViewModel>();
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
        ResizeToContent();
        ApplyTheme();
    }

    private void ResizeToContent()
    {
        var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var scale = GetDpiForWindow(hwnd) / 96.0;
        AppWindow.Resize(new SizeInt32((int)(640 * scale), (int)(800 * scale)));
    }

    private void ApplyTheme()
    {
        var theme = App.Services.GetRequiredService<ISettingsService>().Current.Theme;
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }

    public Visibility VisWhenTrue(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    public Visibility VisWhenFalse(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsDialog { XamlRoot = Content.XamlRoot };
        await dialog.ShowAsync();
    }

    private async void About_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialog { XamlRoot = Content.XamlRoot };
        await dialog.ShowAsync();
    }
}
