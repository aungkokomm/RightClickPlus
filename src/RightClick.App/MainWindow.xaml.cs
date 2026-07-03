using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using RightClick.Core.Models;
using RightClick.Core.Services;
using RightClick_App.ViewModels;

namespace RightClick_App;

public sealed partial class MainWindow : Window
{
    // Fixed content width for this compact utility. The window hugs its content at this width;
    // the height is fitted to the content at runtime.
    private const int ContentWidth = 660;

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
        AppWindow.SetIcon(WindowHelpers.AssetPath("AppIcon.ico"));

        // This is a compact utility, not a document window — a handful of toggles that always fit on
        // screen. Make it a fixed-size window (no maximize, no resize) so the content can never be
        // stranded in a huge empty frame or hidden behind a scrollbar. The window just hugs its
        // content at a comfortable fixed width.
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        AppWindow.ResizeScaled(ContentWidth, 640); // initial; height refined to the content just below

        // Width is fixed, so the content's height is stable and fitting it can't cause a resize loop.
        // Re-fits when the InfoBar opens/closes. TitleBar row is 48; +2 avoids a hairline scrollbar.
        ContentPanel.SizeChanged += (_, e) => AppWindow.FitToContent(ContentWidth, 48 + e.NewSize.Height + 2);

        ApplyTheme();
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
