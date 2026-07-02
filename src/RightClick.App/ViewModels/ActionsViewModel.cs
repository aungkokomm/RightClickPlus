using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RightClick.Core.Models;
using RightClick.Core.Services;

namespace RightClick_App.ViewModels;

/// <summary>
/// Backs the Actions page: three toggles that install or remove RightClick+'s context-menu verbs.
/// Toggle state is sourced from the actual registry so it reflects reality even after manual edits.
/// </summary>
public partial class ActionsViewModel : ObservableObject
{
    private readonly IShellVerbService _verbs;
    private readonly ISettingsService _settings;
    private readonly ILogger<ActionsViewModel> _logger;
    private bool _loading;

    public ActionsViewModel(IShellVerbService verbs, ISettingsService settings, ILogger<ActionsViewModel> logger)
    {
        _verbs = verbs;
        _settings = settings;
        _logger = logger;

        _loading = true;
        FileToFolderEnabled = _verbs.IsInstalled(ShellAction.FileToFolder);
        FileNotesEnabled = _verbs.IsInstalled(ShellAction.FileNotes);
        CopyFileNamesEnabled = _verbs.IsInstalled(ShellAction.CopyFileNames);
        FolderColorEnabled = _verbs.IsInstalled(ShellAction.FolderColor);
        _loading = false;
    }

    [ObservableProperty]
    public partial bool FileToFolderEnabled { get; set; }

    [ObservableProperty]
    public partial bool FileNotesEnabled { get; set; }

    [ObservableProperty]
    public partial bool CopyFileNamesEnabled { get; set; }

    [ObservableProperty]
    public partial bool FolderColorEnabled { get; set; }

    partial void OnFileToFolderEnabledChanged(bool value) => Apply(ShellAction.FileToFolder, value);
    partial void OnFileNotesEnabledChanged(bool value) => Apply(ShellAction.FileNotes, value);
    partial void OnCopyFileNamesEnabledChanged(bool value) => Apply(ShellAction.CopyFileNames, value);
    partial void OnFolderColorEnabledChanged(bool value) => Apply(ShellAction.FolderColor, value);

    private void Apply(ShellAction action, bool enabled)
    {
        if (_loading)
        {
            return;
        }

        try
        {
            if (enabled)
            {
                _verbs.Install(action);
            }
            else
            {
                _verbs.Uninstall(action);
            }

            var s = _settings.Current;
            switch (action)
            {
                case ShellAction.FileToFolder: s.FileToFolderEnabled = enabled; break;
                case ShellAction.FileNotes: s.FileNotesEnabled = enabled; break;
                case ShellAction.CopyFileNames: s.CopyFileNamesEnabled = enabled; break;
                case ShellAction.FolderColor: s.FolderColorEnabled = enabled; break;
            }
            _ = _settings.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to {Op} verb for {Action}.", enabled ? "install" : "remove", action);

            // The registry write failed — snap the toggle back to the real state so the UI never lies.
            _loading = true;
            try { SetEnabled(action, _verbs.IsInstalled(action)); }
            finally { _loading = false; }
        }
    }

    private void SetEnabled(ShellAction action, bool value)
    {
        switch (action)
        {
            case ShellAction.FileToFolder: FileToFolderEnabled = value; break;
            case ShellAction.FileNotes: FileNotesEnabled = value; break;
            case ShellAction.CopyFileNames: CopyFileNamesEnabled = value; break;
            case ShellAction.FolderColor: FolderColorEnabled = value; break;
        }
    }
}
