using System.Runtime.InteropServices;

namespace RightClick.Cli;

// The Windows shell interfaces Explorer uses to run a DelegateExecute verb (see ComServer).

[ComImport, Guid("00000001-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IClassFactory
{
    [PreserveSig] int CreateInstance(IntPtr outer, ref Guid riid, out IntPtr ppv);
    [PreserveSig] int LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
}

[ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyBag
{
    [PreserveSig] int Read([MarshalAs(UnmanagedType.LPWStr)] string name, ref object? value, IntPtr errorLog);
    [PreserveSig] int Write([MarshalAs(UnmanagedType.LPWStr)] string name, ref object value);
}

[ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
    void GetParent(out IShellItem ppsi);
    [PreserveSig] int GetDisplayName(uint sigdn, out IntPtr name);
    void GetAttributes(uint mask, out uint attribs);
    void Compare(IShellItem psi, uint hint, out int order);
}

[ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItemArray
{
    void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
    void GetPropertyStore(int flags, ref Guid riid, out IntPtr ppv);
    void GetPropertyDescriptionList(IntPtr keyType, ref Guid riid, out IntPtr ppv);
    void GetAttributes(int flags, uint mask, out uint attribs);
    void GetCount(out uint count);
    void GetItemAt(uint index, out IShellItem item);
    void EnumItems(out IntPtr ppenum);
}

[ComImport, Guid("85075ACF-231F-40EA-9610-D26B7B58F638"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IInitializeCommand
{
    void Initialize([MarshalAs(UnmanagedType.LPWStr)] string commandName, IPropertyBag? bag);
}

[ComImport, Guid("1C9CD5BB-98E9-4491-A60F-31AACC72B83C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IObjectWithSelection
{
    void SetSelection(IShellItemArray items);
    [PreserveSig] int GetSelection(ref Guid riid, out IntPtr ppv);
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
}

[ComImport, Guid("7F9185B0-CB92-43C5-80A9-92277A4F7B54"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IExecuteCommand
{
    void SetKeyState(uint keyState);
    void SetParameters([MarshalAs(UnmanagedType.LPWStr)] string parameters);
    void SetPosition(POINT pt);
    void SetShowWindow(int show);
    void SetNoShowUI([MarshalAs(UnmanagedType.Bool)] bool noShowUI);
    void SetDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
    void Execute();
}
