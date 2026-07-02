using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RightClick.Core.Models;
using RightClick.Core.Services;

namespace RightClick_App.ViewModels;

/// <summary>
/// Backs the Settings page. Each selection persists immediately.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ILogger<SettingsViewModel> _logger;
    private bool _loading;

    public SettingsViewModel(ISettingsService settings, ILogger<SettingsViewModel> logger)
    {
        _settings = settings;
        _logger = logger;

        _loading = true;
        var s = _settings.Current;
        ThemeIndex = (int)s.Theme;
        NoteFormatIndex = (int)s.NoteFormat;
        NoteEditorIndex = (int)s.NoteEditor;
        CopySeparatorIndex = (int)s.CopySeparator;
        _loading = false;
    }

    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    [ObservableProperty]
    public partial int NoteFormatIndex { get; set; }

    [ObservableProperty]
    public partial int NoteEditorIndex { get; set; }

    [ObservableProperty]
    public partial int CopySeparatorIndex { get; set; }

    partial void OnThemeIndexChanged(int value)
    {
        if (_loading) return;
        _settings.Current.Theme = (AppTheme)value;
        ThemeHelper.Apply((AppTheme)value);
        _ = SaveAsync();
    }

    partial void OnNoteFormatIndexChanged(int value)
    {
        if (_loading) return;
        _settings.Current.NoteFormat = (NoteFormat)value;
        _ = SaveAsync();
    }

    partial void OnNoteEditorIndexChanged(int value)
    {
        if (_loading) return;
        _settings.Current.NoteEditor = (NoteEditor)value;
        _ = SaveAsync();
    }

    partial void OnCopySeparatorIndexChanged(int value)
    {
        if (_loading) return;
        _settings.Current.CopySeparator = (CopySeparator)value;
        _ = SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            await _settings.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist settings change.");
        }
    }
}
