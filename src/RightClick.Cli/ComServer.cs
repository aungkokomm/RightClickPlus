using System.Runtime.InteropServices;
using System.Windows.Forms;
using RightClick.Infrastructure;

namespace RightClick.Cli;

/// <summary>
/// Serves the click handler behind RightClick+'s menu entries (a DelegateExecute verb). Explorer starts
/// this exe with "-Embedding" only when an entry is clicked and passes the whole selection in one call,
/// so any number of selected items is handled by a single process.
/// </summary>
internal static class ComServer
{
    private const uint CLSCTX_LOCAL_SERVER = 4;
    private const uint REGCLS_SINGLEUSE = 0; // one click per process: this process exits once it's done
    private const int IdleTimeoutMs = 30_000;

    // Explorer's calls arrive on COM worker threads, not on this process's main thread. The action is
    // posted to the main (STA) thread's message loop instead: Execute returns to Explorer straight away,
    // and the clipboard gets the STA thread it requires.
    private static SynchronizationContext? _mainThread;

    public static void Run()
    {
        _mainThread = new WindowsFormsSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(_mainThread);

        var clsid = ShellVerbService.CommandHandlerClsid;
        var hr = CoRegisterClassObject(ref clsid, new ClassFactory(), CLSCTX_LOCAL_SERVER, REGCLS_SINGLEUSE, out var cookie);
        if (hr != 0)
        {
            CliLog.Error($"Couldn't serve the click handler (CoRegisterClassObject 0x{hr:X8}).");
            return;
        }

        // Safety net: if Explorer never calls Execute, don't linger.
        using var idle = new System.Windows.Forms.Timer { Interval = IdleTimeoutMs };
        idle.Tick += (_, _) => Application.ExitThread();
        idle.Start();
        try
        {
            Application.Run();
        }
        finally
        {
            CoRevokeClassObject(cookie);
        }
    }

    /// <summary>Runs the action on the main thread once Execute has returned, then lets the process exit.</summary>
    public static void RunThenExit(string command, IReadOnlyList<string> paths) =>
        _mainThread!.Post(_ =>
        {
            try
            {
                Commands.Run(command, paths);
            }
            finally
            {
                Application.ExitThread();
            }
        }, null);

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(ref Guid clsid, [MarshalAs(UnmanagedType.IUnknown)] object unk, uint context, uint flags, out uint cookie);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint cookie);
}

[ComVisible(true)]
internal sealed class ClassFactory : IClassFactory
{
    private const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);

    public int CreateInstance(IntPtr outer, ref Guid riid, out IntPtr ppv)
    {
        ppv = IntPtr.Zero;
        if (outer != IntPtr.Zero)
        {
            return CLASS_E_NOAGGREGATION;
        }

        var unknown = Marshal.GetIUnknownForObject(new ClickHandler());
        try
        {
            return Marshal.QueryInterface(unknown, in riid, out ppv);
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }

    public int LockServer(bool fLock) => 0;
}

/// <summary>
/// One click on a RightClick+ menu entry. Explorer tells it which entry (through the verb's registry
/// values), hands it the selection, then calls Execute.
/// </summary>
[ComVisible(true)]
internal sealed class ClickHandler : IInitializeCommand, IObjectWithSelection, IExecuteCommand
{
    private const int E_NOTIMPL = unchecked((int)0x80004001);
    private const uint SIGDN_FILESYSPATH = 0x80058000;

    private string _command = string.Empty;
    private IShellItemArray? _selection;

    public void Initialize(string commandName, IPropertyBag? bag)
    {
        object? value = null;
        if (bag?.Read(ShellVerbService.CommandValueName, ref value, IntPtr.Zero) == 0 && value is string text)
        {
            // The shell's registry property bag can hand back the string's terminator (and a stray
            // character after it), so keep only what comes before the first NUL.
            var end = text.IndexOf('\0');
            _command = end >= 0 ? text[..end] : text;
        }

        if (_command.Length == 0)
        {
            CliLog.Error($"The '{commandName}' menu entry has no {ShellVerbService.CommandValueName} value.");
        }
    }

    public void SetSelection(IShellItemArray items) => _selection = items;

    public int GetSelection(ref Guid riid, out IntPtr ppv)
    {
        ppv = IntPtr.Zero;
        return E_NOTIMPL;
    }

    public void SetKeyState(uint keyState)
    {
    }

    public void SetParameters(string parameters)
    {
    }

    public void SetPosition(POINT pt)
    {
    }

    public void SetShowWindow(int show)
    {
    }

    public void SetNoShowUI(bool noShowUI)
    {
    }

    public void SetDirectory(string directory)
    {
    }

    public void Execute()
    {
        try
        {
            // Read the paths now: the selection lives in Explorer and is released once this call returns.
            ComServer.RunThenExit(_command, ReadPaths(_selection));
        }
        catch (Exception ex)
        {
            // COM would swallow this silently; record it.
            CliLog.Error($"Running the '{_command}' menu entry failed.", ex);
            throw;
        }
    }

    private static List<string> ReadPaths(IShellItemArray? selection)
    {
        var paths = new List<string>();
        if (selection is null)
        {
            return paths;
        }

        selection.GetCount(out var count);
        for (uint i = 0; i < count; i++)
        {
            selection.GetItemAt(i, out var item);
            if (item.GetDisplayName(SIGDN_FILESYSPATH, out var path) == 0) // items with no file path are skipped
            {
                paths.Add(Marshal.PtrToStringUni(path)!);
                Marshal.FreeCoTaskMem(path);
            }
        }
        return paths;
    }
}
