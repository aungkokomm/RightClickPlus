using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace RightClick_App.ViewModels;

/// <summary>
/// Backs the built-in note editor window. Loads a note file, tracks unsaved changes,
/// and writes it back. Kept deliberately small — a clean, themed alternative to Notepad.
/// </summary>
public partial class NoteEditorViewModel : ObservableObject
{
    private readonly ILogger<NoteEditorViewModel> _logger;
    private string _filePath = string.Empty;
    private bool _loading;

    public NoteEditorViewModel(ILogger<NoteEditorViewModel> logger)
    {
        _logger = logger;
        FileName = string.Empty;
        Content = string.Empty;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    public partial string FileName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WordCount))]
    public partial string Content { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsDirty { get; set; }

    public string DisplayTitle => $"{(IsDirty ? "• " : string.Empty)}{FileName} - Note";

    public string WordCount
    {
        get
        {
            var chars = Content.Length;
            var words = Content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            return $"{words} words · {chars} chars";
        }
    }

    public async Task LoadAsync(string path)
    {
        _filePath = path;
        FileName = Path.GetFileName(path);
        _loading = true;
        try
        {
            Content = File.Exists(path) ? await File.ReadAllTextAsync(path) : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load note {Path}.", path);
            Content = string.Empty;
        }
        finally
        {
            _loading = false;
            IsDirty = false;
        }
    }

    partial void OnContentChanged(string value)
    {
        if (!_loading)
        {
            IsDirty = true;
        }
    }

    private bool CanSave() => IsDirty;

    [RelayCommand(CanExecute = nameof(CanSave))]
    public async Task SaveAsync()
    {
        try
        {
            await File.WriteAllTextAsync(_filePath, Content).ConfigureAwait(true);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save note {Path}.", _filePath);
        }
    }

    /// <summary>
    /// Synchronous save used on the window-close path. Blocking on the async SaveAsync from the UI
    /// thread there can deadlock (its continuation needs the very thread that's waiting), which would
    /// silently lose the note. On failure IsDirty stays set, so the unsaved marker remains visible.
    /// </summary>
    public void Save()
    {
        try
        {
            File.WriteAllText(_filePath, Content);
            IsDirty = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save note {Path}.", _filePath);
        }
    }

    public string TimestampSnippet() => DateTime.Now.ToString("yyyy-MM-dd HH:mm");
}
