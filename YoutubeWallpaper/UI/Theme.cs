using System.Drawing.Drawing2D;

namespace YoutubeWallpaper.UI;

internal static class Theme
{
    public static readonly Color Canvas = Color.FromArgb(20, 29, 35);
    public static readonly Color Surface = Color.FromArgb(38, 53, 63);
    public static readonly Color Text = Color.FromArgb(237, 244, 248);
    public static readonly Color Muted = Color.FromArgb(168, 185, 196);
    public static readonly Color Cyan = Color.FromArgb(117, 215, 232);
    public static readonly Color Violet = Color.FromArgb(178, 158, 244);
    public static readonly Color Green = Color.FromArgb(134, 223, 184);
    public static readonly Color Error = Color.FromArgb(255, 176, 168);

    public static float Scale(Control control, float value) => value * control.DeviceDpi / 96f;
    public static Color Mix(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (int)(from.A + (to.A - from.A) * amount),
            (int)(from.R + (to.R - from.R) * amount),
            (int)(from.G + (to.G - from.G) * amount),
            (int)(from.B + (to.B - from.B) * amount));
    }
    public static GraphicsPath Round(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 0) return path;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
    public static void Prepare(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
    }
}
