using Microsoft.UI.Xaml;
using RightClick.Core.Models;

namespace RightClick_App;

/// <summary>
/// Applies the user's theme preference to the app's root element.
/// </summary>
public static class ThemeHelper
{
    public static void Apply(AppTheme theme)
    {
        if (App.Current.MainWindow?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }
}
