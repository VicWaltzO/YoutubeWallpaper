using System.Diagnostics;
using System.Runtime.InteropServices;

namespace YoutubeWallpaper.UI;

// Short, event-driven transitions. No render loop remains active in the hidden panel.
internal sealed class Motion : IDisposable
{
    private readonly Control owner;
    private readonly Func<float, bool> advance;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Stopwatch clock = new();
    private bool disposed;
    internal bool IsRunning => !disposed && timer.Enabled;

    public Motion(Control owner, Func<float, bool> advance)
    {
        this.owner = owner;
        this.advance = advance;
        timer.Tick += Tick;
        owner.VisibleChanged += VisibilityChanged;
    }
    public static bool Enabled
    {
        get
        {
            bool enabled = true;
            return !SystemParametersInfo(0x1042, 0, ref enabled, 0) || enabled; // GETCLIENTAREAANIMATION
        }
    }
    public static float Approach(float current, float target, float elapsed)
    {
        float next = current + (target - current) * (float.IsPositiveInfinity(elapsed) ? 1 : 1 - MathF.Exp(-22 * elapsed));
        return Math.Abs(next - target) < 0.002f ? target : next;
    }
    public void Request()
    {
        if (disposed) return;
        if (!owner.IsHandleCreated || !owner.Visible || !Enabled)
        {
            timer.Stop();
            advance(float.PositiveInfinity);
            owner.Invalidate();
            return;
        }
        if (!timer.Enabled)
        {
            clock.Restart();
            timer.Start();
        }
    }
    private void Tick(object? sender, EventArgs e)
    {
        if (!owner.Visible || owner.IsDisposed) { timer.Stop(); return; }
        float elapsed = Math.Clamp((float)clock.Elapsed.TotalSeconds, 0.001f, 0.05f);
        clock.Restart();
        bool active = advance(elapsed);
        owner.Invalidate();
        if (!active) timer.Stop();
    }
    private void VisibilityChanged(object? sender, EventArgs e)
    {
        if (!owner.Visible)
        {
            timer.Stop();
            advance(float.PositiveInfinity);
        }
        else Request();
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        owner.VisibleChanged -= VisibilityChanged;
        timer.Dispose();
    }
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint action, uint parameter,
        [MarshalAs(UnmanagedType.Bool)] ref bool value, uint flags);
}
