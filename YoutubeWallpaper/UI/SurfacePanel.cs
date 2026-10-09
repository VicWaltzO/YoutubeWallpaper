using System.Drawing.Drawing2D;

namespace YoutubeWallpaper.UI;

internal sealed class SurfacePanel : Panel
{
    private readonly Motion motion;
    private float focus;
    private bool highlighted;
    public Color TopColor { get; set; } = Color.FromArgb(49, 66, 78);
    public Color BottomColor { get; set; } = Color.FromArgb(36, 50, 61);
    public Color Accent { get; set; } = Theme.Cyan;
    public int CornerRadius { get; set; } = 18;
    public bool HasShadow { get; set; } = true;
    public bool Highlighted
    {
        get => highlighted;
        set { highlighted = value; motion.Request(); }
    }
    public SurfacePanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        motion = new(this, elapsed =>
        {
            focus = Motion.Approach(focus, highlighted ? 1 : 0, elapsed);
            return focus != (highlighted ? 1 : 0);
        });
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Theme.Prepare(e.Graphics);
        float inset = Theme.Scale(this, HasShadow ? 4 : 1);
        var bounds = new RectangleF(inset, inset, Width - inset * 2 - 1, Height - inset * 2 - 1);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        float radius = Theme.Scale(this, CornerRadius);
        if (HasShadow)
        {
            var shadow = bounds;
            shadow.Offset(0, Theme.Scale(this, 3));
            using var shadowPath = Theme.Round(shadow, radius);
            using var shadowBrush = new SolidBrush(Color.FromArgb(70, 8, 15, 22));
            e.Graphics.FillPath(shadowBrush, shadowPath);
        }
        using var path = Theme.Round(bounds, radius);
        using var fill = new LinearGradientBrush(bounds, TopColor, BottomColor, 115);
        e.Graphics.FillPath(fill, path);
        using var border = new Pen(Theme.Mix(Color.FromArgb(85, 94, 117, 134), Accent, focus * 0.75f), Theme.Scale(this, 1));
        e.Graphics.DrawPath(border, path);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) motion.Dispose();
        base.Dispose(disposing);
    }
}
