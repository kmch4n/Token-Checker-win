using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Forms;

namespace UsageBeacon.Utilities;

/// <summary>
/// Reads taskbar and notification-area geometry through Windows APIs.
/// </summary>
public static class TaskbarPosition
{
    [DllImport("user32.dll")] private static extern IntPtr FindWindow(string cls, string? win);
    [DllImport("user32.dll")] private static extern IntPtr FindWindowEx(IntPtr p, IntPtr a, string cls, string? win);
    [DllImport("user32.dll")] private static extern bool   EnumWindows(EnumWindowsProc proc, IntPtr parameter);
    [DllImport("user32.dll")] private static extern int    GetClassName(IntPtr hwnd, StringBuilder name, int count);
    [DllImport("user32.dll")] private static extern bool   GetWindowRect(IntPtr hwnd, out RECT r);

    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    public record Info(
        double TaskbarTop,    double TaskbarLeft,
        double TaskbarBottom, double TaskbarRight,
        double TaskbarHeight,
        double NotifyLeft,     // Notification area's left edge in WPF logical pixels.
        double? WidgetsRight,  // Right edge of the weather/widgets area.
        double? ContentLeft,   // Left edge of centered taskbar items.
        double? ContentRight); // Right edge of centered taskbar items.

    public static Info? Get(int screenIndex = 0)
    {
        var screens = Screen.AllScreens;
        var target = screenIndex < screens.Length ? screens[screenIndex] : screens[0];
        var taskbar = FindTaskbar(target.Bounds);
        if (taskbar == IntPtr.Zero) return null;

        if (!GetWindowRect(taskbar, out var tb)) return null;

        // An auto-hidden taskbar can be four pixels or smaller.
        // Return null so the caller can fall back to screen-edge placement.
        if (tb.Bottom - tb.Top <= 4 || tb.Right - tb.Left <= 4) return null;

        // Place the widget to the left of the notification area.
        var notify = FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        int notifyLeft = tb.Right;
        if (notify != IntPtr.Zero && GetWindowRect(notify, out var nr))
            notifyLeft = nr.Left;

        using var g = Graphics.FromHwnd(taskbar);
        var dpi = g.DpiX / 96.0;
        var widgetsRight = TryGetLeftWidgetsRight(taskbar, tb, dpi);
        var rightAnchorLeft = notify != IntPtr.Zero
            ? notifyLeft / dpi
            : TryGetClockLeft(taskbar, tb, dpi) ?? tb.Right / dpi;
        var contentBounds = TryGetContentBounds(taskbar, tb, dpi, widgetsRight, rightAnchorLeft);

        return new Info(
            TaskbarTop:    tb.Top    / dpi,
            TaskbarLeft:   tb.Left   / dpi,
            TaskbarBottom: tb.Bottom / dpi,
            TaskbarRight:  tb.Right  / dpi,
            TaskbarHeight: (tb.Bottom - tb.Top) / dpi,
            NotifyLeft:    rightAnchorLeft,
            WidgetsRight:  widgetsRight,
            ContentLeft:   contentBounds.Left,
            ContentRight:  contentBounds.Right);
    }

    private static IntPtr FindTaskbar(Rectangle targetBounds)
    {
        var match = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            var className = new StringBuilder(64);
            GetClassName(hwnd, className, className.Capacity);
            if (className.ToString() is not ("Shell_TrayWnd" or "Shell_SecondaryTrayWnd") ||
                !GetWindowRect(hwnd, out var rect))
                return true;

            var bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            if (!bounds.IntersectsWith(targetBounds)) return true;

            match = hwnd;
            return false;
        }, IntPtr.Zero);
        return match;
    }

    private static double? TryGetLeftWidgetsRight(IntPtr taskbar, RECT taskbarRect, double dpi)
    {
        try
        {
            var root = AutomationElement.FromHandle(taskbar);
            var buttons = root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));

            double? right = null;
            foreach (AutomationElement button in buttons)
            {
                var rect = button.Current.BoundingRectangle;
                if (rect.IsEmpty ||
                    rect.Left > taskbarRect.Left + 250 ||
                    rect.Top < taskbarRect.Top ||
                    rect.Bottom > taskbarRect.Bottom)
                    continue;

                right = right is null ? rect.Right / dpi : Math.Max(right.Value, rect.Right / dpi);
            }
            return right;
        }
        catch { return null; }
    }

    private static double? TryGetClockLeft(IntPtr taskbar, RECT taskbarRect, double dpi)
    {
        try
        {
            var root = AutomationElement.FromHandle(taskbar);
            var buttons = root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));

            double? rightmostLeft = null;
            double? rightmostEdge = null;
            foreach (AutomationElement button in buttons)
            {
                var rect = button.Current.BoundingRectangle;
                if (rect.IsEmpty ||
                    rect.Top < taskbarRect.Top ||
                    rect.Bottom > taskbarRect.Bottom ||
                    rect.Left < taskbarRect.Left + (taskbarRect.Right - taskbarRect.Left) / 2.0)
                    continue;

                if (rightmostEdge is null || rect.Right > rightmostEdge)
                {
                    rightmostEdge = rect.Right;
                    rightmostLeft = rect.Left / dpi;
                }
            }
            return rightmostLeft;
        }
        catch { }
        return null;
    }

    private static (double? Left, double? Right) TryGetContentBounds(
        IntPtr taskbar, RECT taskbarRect, double dpi, double? widgetsRight, double rightAnchorLeft)
    {
        try
        {
            var root = AutomationElement.FromHandle(taskbar);
            var buttons = root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));

            var afterWidgets = (widgetsRight ?? taskbarRect.Left / dpi) + 1;
            double? left = null;
            double? right = null;
            foreach (AutomationElement button in buttons)
            {
                var rect = button.Current.BoundingRectangle;
                var rectLeft = rect.Left / dpi;
                var rectRight = rect.Right / dpi;
                if (rect.IsEmpty ||
                    rect.Top < taskbarRect.Top ||
                    rect.Bottom > taskbarRect.Bottom ||
                    rectLeft <= afterWidgets ||
                    rectRight >= rightAnchorLeft)
                    continue;

                left = left is null ? rectLeft : Math.Min(left.Value, rectLeft);
                right = right is null ? rectRight : Math.Max(right.Value, rectRight);
            }
            return (left, right);
        }
        catch { return (null, null); }
    }
}
