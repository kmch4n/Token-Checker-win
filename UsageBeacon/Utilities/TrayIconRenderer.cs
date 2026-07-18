using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace UsageBeacon.Utilities;

/// <summary>
/// Renders a tray icon colored by Claude and Codex utilization.
/// </summary>
public static class TrayIconRenderer
{
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon CreateIcon(double? claudeUtil, double? codexUtil)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size);

        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            DrawBar(g, x: 1,  width: 13, maxHeight: 26, yTop: 3, util: claudeUtil);
            DrawBar(g, x: 18, width: 13, maxHeight: 26, yTop: 3, util: codexUtil);
        }

        // Icon.FromHandle does not own the native HICON returned by GetHicon.
        // Clone a managed icon, then release the native handle explicitly.
        var hIcon = bmp.GetHicon();
        try
        {
            using var tmp = Icon.FromHandle(hIcon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    private static void DrawBar(Graphics g, int x, int width, int maxHeight, int yTop, double? util)
    {
        var fillColor  = UsageColor(util);
        var trackColor = Color.FromArgb(60, fillColor);
        var fillHeight = util.HasValue ? (int)(maxHeight * Math.Clamp(util.Value, 0, 1)) : 0;

        // Background track.
        using (var brush = new SolidBrush(trackColor))
            g.FillRectangle(brush, x, yTop, width, maxHeight);

        // Utilization fill grows from the bottom.
        if (fillHeight > 0)
        {
            using var brush = new SolidBrush(fillColor);
            g.FillRectangle(brush, x, yTop + maxHeight - fillHeight, width, fillHeight);
        }

        // Border.
        using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
            g.DrawRectangle(pen, x, yTop, width - 1, maxHeight - 1);
    }

    private static Color UsageColor(double? util)
    {
        if (util == null)         return Color.FromArgb(0x80, 0x80, 0x80); // Gray
        if (util < 0.75)          return Color.FromArgb(0x4C, 0xAF, 0x50); // Green
        if (util < 0.90)          return Color.FromArgb(0xFF, 0xC1, 0x07); // Yellow
        return                           Color.FromArgb(0xF4, 0x43, 0x36); // Red
    }

}
