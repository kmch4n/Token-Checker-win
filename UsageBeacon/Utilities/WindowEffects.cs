using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MediaColor = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace UsageBeacon.Utilities;

public static class WindowEffects
{
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE      = 38;  // Win11 22H2+
    private const int DWMWCP_ROUNDSMALL              = 3;
    private const int DWMSBT_TRANSIENTWINDOW         = 3;

    [DllImport("dwmapi.dll", PreserveSig = false)]
    private static extern void DwmSetWindowAttribute(
        IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(
        IntPtr hwnd, ref WindowCompositionAttributeData data);

    /// <summary>
    /// Applies a DWM acrylic backdrop to a normal, non-layered HWND.
    /// </summary>
    public static void Apply(Window window, bool lightMode = false)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            window.SourceInitialized += (_, _) => Apply(window, lightMode);
            return;
        }

        // Extend the DWM frame across the client area so DWMSBT_TRANSIENTWINDOW can work.
        TryExtendFrame(hwnd);

        // Windows 11 rounded corners.
        TryDwm(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, DWMWCP_ROUNDSMALL);

        // Windows 11 22H2+ acrylic system backdrop.
        TryDwm(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, DWMSBT_TRANSIENTWINDOW);

        // Windows 10 fallback with light translucent blur.
        uint gradient = lightMode ? 0x28_F0_F0_F0u : 0x28_10_10_10u;
        TryWin10Acrylic(hwnd, gradient);

        // Keep the WPF background transparent so the DWM backdrop remains visible.
        window.Background = new SolidColorBrush(MediaColor.FromArgb(1, 0, 0, 0));
    }

    private static void TryExtendFrame(IntPtr hwnd)
    {
        try
        {
            var m = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(hwnd, ref m);
        }
        catch { }
    }

    private static void TryDwm(IntPtr hwnd, int attr, int value)
    {
        try { DwmSetWindowAttribute(hwnd, attr, ref value, sizeof(int)); } catch { }
    }

    private static void TryWin10Acrylic(IntPtr hwnd, uint gradientColor)
    {
        try
        {
            var accent = new AccentPolicy
            {
                AccentState   = 4,  // ACCENT_ENABLE_ACRYLICBLURBEHIND
                AccentFlags   = 2,
                GradientColor = (int)gradientColor,
            };
            var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(accent));
            try
            {
                Marshal.StructureToPtr(accent, ptr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute  = 19,   // WCA_ACCENT_POLICY
                    Data       = ptr,
                    SizeOfData = Marshal.SizeOf(accent),
                };
                SetWindowCompositionAttribute(hwnd, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }
        catch { }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState, AccentFlags, GradientColor, AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int    Attribute;
        public IntPtr Data;
        public int    SizeOfData;
    }
}
