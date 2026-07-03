using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RightClick.Core.Services;
using RightClick_App.ViewModels;
using Windows.System;

namespace RightClick_App;

/// <summary>
/// RightClick+'s built-in note editor — a clean, themed alternative to Notepad for the File Note action.
/// </summary>
public sealed partial class NoteEditorWindow : Window
{
    private bool _ready;

    public NoteEditorViewModel ViewModel { get; }

    public NoteEditorWindow(string notePath)
    {
        ViewModel = App.Services.GetRequiredService<NoteEditorViewModel>();
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon(WindowHelpers.AssetPath("file-notes.ico"));
        AppWindow.ResizeScaled(720, 640);

        // Match the user's chosen theme.
        var theme = App.Services.GetRequiredService<ISettingsService>().Current.Theme;
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                RightClick.Core.Models.AppTheme.Light => ElementTheme.Light,
                RightClick.Core.Models.AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }

        // Ctrl+S to save.
        var save = new KeyboardAccelerator { Key = VirtualKey.S, Modifiers = VirtualKeyModifiers.Control };
        save.Invoked += (sender, e) => { e.Handled = true; _ = ViewModel.SaveAsync(); };
        (Content as FrameworkElement)!.KeyboardAccelerators.Add(save);

        // Keep the OS window title (taskbar / alt-tab) in sync with the note name + dirty state.
        Title = ViewModel.DisplayTitle;
        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NoteEditorViewModel.DisplayTitle))
            {
                Title = ViewModel.DisplayTitle;
            }
        };

        Closed += OnClosed;
        _ = InitAsync(notePath);
    }

    private async Task InitAsync(string notePath)
    {
        await ViewModel.LoadAsync(notePath);
        Editor.Text = ViewModel.Content;
        _ready = true;
        Editor.Focus(FocusState.Programmatic);
    }

    private void Editor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_ready)
        {
            ViewModel.Content = Editor.Text;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e) => _ = ViewModel.SaveAsync();

    private void InsertTimestamp_Click(object sender, RoutedEventArgs e)
    {
        var stamp = ViewModel.TimestampSnippet();
        var start = Editor.SelectionStart;
        Editor.Text = Editor.Text.Remove(start, Editor.SelectionLength).Insert(start, stamp);
        Editor.SelectionStart = start + stamp.Length;
        Editor.SelectionLength = 0;
        Editor.Focus(FocusState.Programmatic);
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        // Auto-save any unsaved changes so a quick note is never lost. Use the synchronous save here:
        // blocking on the async one from the closing UI thread can deadlock and drop the note.
        if (ViewModel.IsDirty)
        {
            ViewModel.Save();
        }
    }
}
