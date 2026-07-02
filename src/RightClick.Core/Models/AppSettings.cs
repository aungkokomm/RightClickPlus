namespace RightClick.Core.Models;

/// <summary>File-note sidecar format.</summary>
public enum NoteFormat
{
    PlainText, // .txt
    Markdown,  // .md
}

/// <summary>Separator used when copying multiple file names/paths to the clipboard.</summary>
public enum CopySeparator
{
    NewLine,
    Comma,
}

/// <summary>App theme preference.</summary>
public enum AppTheme
{
    System,
    Light,
    Dark,
}

/// <summary>Which editor opens a file note.</summary>
public enum NoteEditor
{
    BuiltIn, // RightClick+'s own editor window
    Notepad, // the system Notepad
}

/// <summary>
/// Persisted user settings. Plain DTO serialized to JSON by the settings service.
/// </summary>
public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;
    public NoteFormat NoteFormat { get; set; } = NoteFormat.PlainText;
    public NoteEditor NoteEditor { get; set; } = NoteEditor.BuiltIn;
    public CopySeparator CopySeparator { get; set; } = CopySeparator.NewLine;

    /// <summary>Whether each enabled action verb is currently installed in the context menu.</summary>
    public bool FileToFolderEnabled { get; set; }
    public bool FileNotesEnabled { get; set; }
    public bool CopyFileNamesEnabled { get; set; }
    public bool FolderColorEnabled { get; set; }
}
