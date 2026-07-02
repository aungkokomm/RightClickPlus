using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using RightClick_App.ViewModels;

namespace RightClick_App;

public sealed partial class SettingsDialog : ContentDialog
{
    public SettingsViewModel ViewModel { get; }

    public SettingsDialog()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }
}
