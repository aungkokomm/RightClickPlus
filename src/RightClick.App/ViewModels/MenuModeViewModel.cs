using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using RightClick.Core.Models;
using RightClick.Core.Services;

namespace RightClick_App.ViewModels;

/// <summary>
/// Drives the Menu Mode page: shows the current context-menu style and switches between
/// the Windows 11 modern and classic menus (restarting Explorer to apply).
/// </summary>
public partial class MenuModeViewModel : ObservableObject
{
    private readonly IContextMenuTweakService _tweak;
    private readonly ILogger<MenuModeViewModel> _logger;

    public MenuModeViewModel(IContextMenuTweakService tweak, ILogger<MenuModeViewModel> logger)
    {
        _tweak = tweak;
        _logger = logger;
        OsDescription = string.Empty;
        InfoTitle = string.Empty;
        InfoMessage = string.Empty;
        InfoSeverity = InfoBarSeverity.Informational;
        Refresh();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentModeText))]
    [NotifyPropertyChangedFor(nameof(IsClassicActive))]
    [NotifyPropertyChangedFor(nameof(IsModernActive))]
    [NotifyPropertyChangedFor(nameof(SwitchButtonText))]
    public partial MenuMode CurrentMode { get; set; }

    public string SwitchButtonText => IsModernActive ? "Use Classic menu" : "Use Modern menu";

    [ObservableProperty]
    public partial bool IsWindows11 { get; set; }

    [ObservableProperty]
    public partial string OsDescription { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SwitchToClassicCommand))]
    [NotifyCanExecuteChangedFor(nameof(SwitchToModernCommand))]
    [NotifyCanExecuteChangedFor(nameof(SwitchMenuCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsInfoOpen { get; set; }

    [ObservableProperty]
    public partial string InfoTitle { get; set; }

    [ObservableProperty]
    public partial string InfoMessage { get; set; }

    [ObservableProperty]
    public partial InfoBarSeverity InfoSeverity { get; set; }

    public bool IsClassicActive => CurrentMode is MenuMode.Classic or MenuMode.NativeClassic;
    public bool IsModernActive => CurrentMode is MenuMode.Modern;

    public string CurrentModeText => CurrentMode switch
    {
        MenuMode.Classic => "Classic (full) menu — forced via tweak",
        MenuMode.Modern => "Modern (compact) Windows 11 menu",
        MenuMode.NativeClassic => "Classic menu (native to this Windows version)",
        _ => "Unknown",
    };

    [RelayCommand]
    private void Refresh()
    {
        var state = _tweak.GetState();
        CurrentMode = state.Mode;
        IsWindows11 = state.IsWindows11;
        OsDescription = state.OsDescription;
    }

    private bool CanSwitch() => !IsBusy && IsWindows11;

    [RelayCommand(CanExecute = nameof(CanSwitch))]
    private Task SwitchToClassicAsync() => ApplyAsync(MenuMode.Classic);

    [RelayCommand(CanExecute = nameof(CanSwitch))]
    private Task SwitchToModernAsync() => ApplyAsync(MenuMode.Modern);

    [RelayCommand(CanExecute = nameof(CanSwitch))]
    private Task SwitchMenuAsync() => ApplyAsync(IsModernActive ? MenuMode.Classic : MenuMode.Modern);

    private async Task ApplyAsync(MenuMode mode)
    {
        IsBusy = true;
        IsInfoOpen = false;
        try
        {
            await _tweak.SetModeAsync(mode, restartExplorer: true);
            Refresh();
            ShowInfo(
                "Done",
                mode == MenuMode.Classic
                    ? "Classic context menu is now active. Explorer was restarted."
                    : "Modern context menu restored. Explorer was restarted.",
                InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch menu mode to {Mode}.", mode);
            ShowInfo("Couldn't switch menu", ex.Message, InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowInfo(string title, string message, InfoBarSeverity severity)
    {
        InfoTitle = title;
        InfoMessage = message;
        InfoSeverity = severity;
        IsInfoOpen = true;
    }
}
