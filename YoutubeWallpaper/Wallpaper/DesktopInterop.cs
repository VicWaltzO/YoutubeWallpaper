using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace YoutubeWallpaper.Wallpaper;

// Explorer's WorkerW topology is undocumented. Never alter or destroy shell windows.
internal static class DesktopInterop
{
    private const int GwlStyle = -16, GwlExStyle = -20;
    private const long WsChild = 0x40000000, WsPopup = 0x80000000;
    private const long WsExAppWindow = 0x40000, WsExToolWindow = 0x80, WsExNoActivate = 0x08000000;
    private const uint SwpNoActivate = 0x10, SwpFrameChanged = 0x20, SwpShowWindow = 0x40;

    internal sealed record DesktopLayer(nint Parent, nint IconRoot, nint IconView, uint ExplorerProcess);

    public static DesktopLayer FindDesktopLayer()
    {
        nint progman = FindWindow("Progman", null);
        if (progman == 0 || !IsWindow(progman))
            throw new InvalidOperationException("No se encuentra el escritorio de Explorer. Vuelve a intentar cuando esté disponible.");
        GetWindowThreadProcessId(progman, out uint explorerProcess);
        if (explorerProcess == 0)
            throw new InvalidOperationException("No se pudo identificar el proceso del escritorio.");

        // Bounded messages request the shell's wallpaper surface, without blocking Explorer.
        if (SendMessageTimeout(progman, 0x052C, (nint)0xD, 0, 0x2, 700, out _) == 0 ||
            SendMessageTimeout(progman, 0x052C, (nint)0xD, (nint)1, 0x2, 700, out _) == 0)
            throw new InvalidOperationException("Explorer no respondió al preparar la capa del escritorio. Intenta aplicar de nuevo.");

        var windows = new List<nint>();
        EnumWindowsProc collect = (window, _) => { windows.Add(window); return true; };
        if (!EnumWindows(collect, 0)) throw NativeError("Enumerar escritorio");
        Point center = CenterOfPrimaryScreen();

        foreach (nint iconRoot in windows)
        {
            if (!BelongsToExplorer(iconRoot, explorerProcess)) continue;
            nint iconView = FindWindowEx(iconRoot, 0, "SHELLDLL_DefView", null);
            if (iconView == 0 || !IsWindowVisible(iconView)) continue;

            // Classic topology: a top-level WorkerW below the top-level icon container.
            int iconIndex = windows.IndexOf(iconRoot);
            foreach (nint candidate in windows.Skip(iconIndex + 1))
            {
                if (IsWallpaperLayer(candidate, explorerProcess, center))
                    return new(candidate, iconRoot, iconView, explorerProcess);
            }

            // Some Windows 11 builds place WorkerW and DefView under Progman.
            // Only accept a visible sibling below DefView; no fixed child order is assumed.
            for (nint sibling = GetWindow(iconView, 2); sibling != 0; sibling = GetWindow(sibling, 2))
            {
                if (GetParent(sibling) == iconRoot && IsWallpaperLayer(sibling, explorerProcess, center))
                    return new(sibling, iconRoot, iconView, explorerProcess);
            }
        }
        throw new InvalidOperationException("No se encontró una capa WorkerW verificable detrás de los iconos. Esta versión de Explorer puede usar otra composición. No se creó un wallpaper; consulta README.");
    }

    private static bool IsWallpaperLayer(nint window, uint process, Point center) =>
        IsWindow(window) && IsWindowVisible(window) && ClassName(window) == "WorkerW" &&
        BelongsToExplorer(window, process) && FindWindowEx(window, 0, "SHELLDLL_DefView", null) == 0 &&
        GetWindowRect(window, out NativeRect rect) && rect.Contains(center);

    private static bool BelongsToExplorer(nint window, uint process)
    {
        GetWindowThreadProcessId(window, out uint actual);
        return actual == process;
    }

    public static IDisposable MatchDpiContext(nint parent) => new DpiScope(parent);

    private sealed class DpiScope : IDisposable
    {
        private readonly nint previous;
        public DpiScope(nint parent)
        {
            nint context = GetWindowDpiAwarenessContext(parent);
            if (context == 0) throw NativeError("Consultar DPI del escritorio");
            previous = SetThreadDpiAwarenessContext(context);
            if (previous == 0) throw NativeError("Ajustar DPI de la superficie");
        }
        public void Dispose() => SetThreadDpiAwarenessContext(previous);
    }

    public static void Attach(nint window, DesktopLayer layer)
    {
        if (!IsWindow(window) || !IsLayerAlive(layer)) throw new InvalidOperationException("La capa del escritorio dejó de estar disponible.");
        SetStyle(window, GwlStyle, (GetStyle(window, GwlStyle) & ~WsPopup) | WsChild);
        SetStyle(window, GwlExStyle, (GetStyle(window, GwlExStyle) & ~WsExAppWindow) | WsExToolWindow | WsExNoActivate);
        Marshal.SetLastPInvokeError(0);
        nint oldParent = SetParent(window, layer.Parent);
        if (oldParent == 0 && Marshal.GetLastPInvokeError() != 0) throw NativeError("Acoplar wallpaper");
        if (GetParent(window) != layer.Parent) throw new InvalidOperationException("Explorer no aceptó la superficie de wallpaper.");
        Resize(window, layer);
        // Disabled HWNDs do not take input. This also covers WebView2's native child subtree.
        EnableWindow(window, false);
        if (IsWindowEnabled(window)) throw new InvalidOperationException("No se pudo hacer pasiva la superficie del wallpaper.");
    }

    public static void Resize(nint window, DesktopLayer layer)
    {
        if (!IsWindow(window) || !IsLayerAlive(layer)) throw new InvalidOperationException("El escritorio fue recreado. Aplica el wallpaper de nuevo.");
        Rectangle bounds = Screen.PrimaryScreen?.Bounds ?? throw new InvalidOperationException("No se encuentra el monitor principal.");
        var origin = new NativePoint { X = bounds.X, Y = bounds.Y };
        if (!ScreenToClient(layer.Parent, ref origin)) throw NativeError("Convertir dimensiones del escritorio");
        if (!SetWindowPos(window, (nint)1, origin.X, origin.Y, bounds.Width, bounds.Height,
            SwpNoActivate | SwpFrameChanged | SwpShowWindow)) throw NativeError("Dimensionar wallpaper");
    }

    public static bool IsLayerAlive(DesktopLayer layer) =>
        IsWindow(layer.Parent) && IsWindow(layer.IconRoot) && IsWindow(layer.IconView) &&
        IsWindowVisible(layer.Parent) && ClassName(layer.Parent) == "WorkerW" &&
        FindWindowEx(layer.IconRoot, 0, "SHELLDLL_DefView", null) == layer.IconView &&
        BelongsToExplorer(layer.Parent, layer.ExplorerProcess) &&
        BelongsToExplorer(layer.IconRoot, layer.ExplorerProcess) &&
        IsBelow(GetParent(layer.Parent) == layer.IconRoot ? layer.IconView : layer.IconRoot, layer.Parent);

    private static bool IsBelow(nint reference, nint candidate)
    {
        // Bounded traversal also detects shell reordering while the wallpaper is active.
        nint next = GetWindow(reference, 2);
        for (int count = 0; next != 0 && count < 4096; count++, next = GetWindow(next, 2))
            if (next == candidate) return true;
        return false;
    }

    public static bool IsAttached(nint window, DesktopLayer layer) =>
        IsWindow(window) && IsLayerAlive(layer) && GetParent(window) == layer.Parent;

    public static string ClassName(nint window)
    {
        var name = new StringBuilder(256);
        return GetClassName(window, name, name.Capacity) > 0 ? name.ToString() : "";
    }

    private static Point CenterOfPrimaryScreen()
    {
        Rectangle bounds = Screen.PrimaryScreen?.Bounds ?? throw new InvalidOperationException("No se encuentra el monitor principal.");
        return new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
    }

    private static long GetStyle(nint window, int index)
    {
        Marshal.SetLastPInvokeError(0);
        nint style = IntPtr.Size == 8 ? GetWindowLongPtr(window, index) : (nint)GetWindowLong(window, index);
        if (style == 0 && Marshal.GetLastPInvokeError() != 0) throw NativeError("Leer estilo de ventana");
        return style.ToInt64();
    }

    private static void SetStyle(nint window, int index, long style)
    {
        Marshal.SetLastPInvokeError(0);
        nint result = IntPtr.Size == 8 ? SetWindowLongPtr(window, index, (nint)style) : (nint)SetWindowLong(window, index, unchecked((int)style));
        if (result == 0 && Marshal.GetLastPInvokeError() != 0) throw NativeError("Configurar estilo de ventana");
    }

    private static Win32Exception NativeError(string operation) => new(Marshal.GetLastPInvokeError(), operation + " falló.");

    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
        public readonly bool Contains(Point p) => p.X >= Left && p.X < Right && p.Y >= Top && p.Y < Bottom;
    }
    private delegate bool EnumWindowsProc(nint hwnd, nint parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint FindWindow(string className, string? title);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint FindWindowEx(nint parent, nint after, string className, string? title);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);
    [DllImport("user32.dll", SetLastError = true)] private static extern nint SendMessageTimeout(nint window, uint message, nint wParam, nint lParam, uint flags, uint timeout, out nint result);
    [DllImport("user32.dll", SetLastError = true)] private static extern nint SetParent(nint child, nint parent);
    [DllImport("user32.dll")] private static extern nint GetParent(nint window);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder name, int maxCount);
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(nint window);
    [DllImport("user32.dll")] private static extern bool EnableWindow(nint window, bool enable);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetWindowRect(nint window, out NativeRect rect);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool ScreenToClient(nint window, ref NativePoint point);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong(nint window, int index, int value);
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint window, int index, nint value);
    [DllImport("user32.dll")] private static extern nint GetWindowDpiAwarenessContext(nint window);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(nint window);
    [DllImport("user32.dll", SetLastError = true)] private static extern nint SetThreadDpiAwarenessContext(nint context);
}
