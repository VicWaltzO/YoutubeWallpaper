using System.Runtime.InteropServices;

namespace YoutubeWallpaper.UI;

internal static class WindowAppearance
{
    public static void Apply(nint handle)
    {
        int dark = 1, rounded = 2;
        // Cosmetic hints on our configuration window; fall back to native chrome if unsupported.
        _ = DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        _ = DwmSetWindowAttribute(handle, 33, ref rounded, sizeof(int));
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
}
