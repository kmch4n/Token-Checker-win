using System.Runtime.InteropServices;

namespace UsageBeacon.Utilities;

/// <summary>
/// Wraps Windows virtual desktop APIs for pinning and desktop-follow behavior.
/// </summary>
public static class VirtualDesktopHelper
{
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
    private interface IVirtualDesktopManager
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        bool IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow);
        Guid GetWindowDesktopId(IntPtr topLevelWindow);
        void MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
    }

    [ComImport, Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a")]
    private class CVirtualDesktopManager { }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetPropW(IntPtr hWnd, string lpString, IntPtr hData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint exStyle, string className, string? windowName,
        uint style, int x, int y, int w, int h,
        IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);

    private const uint WS_POPUP = 0x80000000;

    private static readonly IVirtualDesktopManager? _manager;

    static VirtualDesktopHelper()
    {
        try { _manager = (IVirtualDesktopManager)new CVirtualDesktopManager(); }
        catch { }
    }

    /// <summary>
    /// Pins a window to every virtual desktop through SetPropW on Windows 10 and 11.
    /// IsOnCurrentDesktop and MoveToCurrentDesktop provide a fallback when unsupported.
    /// </summary>
    public static void PinToAllDesktops(IntPtr hwnd)
    {
        try { SetPropW(hwnd, "VirtualDesktopPinned", new IntPtr(1)); }
        catch { }
    }

    /// <summary>Checks whether a window belongs to the active virtual desktop.</summary>
    public static bool IsOnCurrentDesktop(IntPtr hwnd)
    {
        if (_manager == null || hwnd == IntPtr.Zero) return true;
        try { return _manager.IsWindowOnCurrentVirtualDesktop(hwnd); }
        catch { return true; }
    }

    /// <summary>
    /// Moves a window to the active virtual desktop.
    /// Uses the foreground window to resolve the desktop ID and a temporary window as fallback.
    /// </summary>
    public static void MoveToCurrentDesktop(IntPtr hwnd)
    {
        if (_manager == null || hwnd == IntPtr.Zero) return;
        try
        {
            var desktopId = GetCurrentDesktopId();
            if (desktopId == Guid.Empty) return;
            _manager.MoveWindowToDesktop(hwnd, ref desktopId);
        }
        catch { }
    }

    private static Guid GetCurrentDesktopId()
    {
        if (_manager == null) return Guid.Empty;

        // Resolve the active desktop from the foreground window.
        var fg = GetForegroundWindow();
        if (fg != IntPtr.Zero)
        {
            try
            {
                var id = _manager.GetWindowDesktopId(fg);
                if (id != Guid.Empty) return id;
            }
            catch { }
        }

        // An empty new desktop has no foreground window, so create a temporary popup.
        // Newly created windows always belong to the active desktop.
        var probe = CreateWindowEx(0, "STATIC", null, WS_POPUP,
            -1, -1, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (probe != IntPtr.Zero)
        {
            try { return _manager.GetWindowDesktopId(probe); }
            catch { }
            finally { DestroyWindow(probe); }
        }

        return Guid.Empty;
    }
}
