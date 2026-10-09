namespace YoutubeWallpaper.UI;

internal sealed class StatusBadge : Label
{
    private readonly Motion motion;
    private bool loading;
    private float phase;
    public bool IsLoading
    {
        get => loading;
        set { if (loading == value) return; loading = value; motion.Request(); }
    }
    public StatusBadge()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        motion = new(this, elapsed =>
        {
            if (!float.IsPositiveInfinity(elapsed)) phase = (phase + elapsed * 5) % (MathF.PI * 2);
            return loading;
        });
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        Theme.Prepare(e.Graphics);
        float unit = DeviceDpi / 96f;
        var bounds = new RectangleF(unit, unit, Width - unit * 2 - 1, Height - unit * 2 - 1);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var path = Theme.Round(bounds, 13 * unit);
        using var fill = new SolidBrush(Color.FromArgb(50, ForeColor));
        using var border = new Pen(Color.FromArgb(65, ForeColor), unit);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        float radius = (loading ? 3 + MathF.Sin(phase) * 0.6f : 3) * unit;
        using var dot = new SolidBrush(ForeColor);
        e.Graphics.FillEllipse(dot, 15 * unit - radius, Height / 2f - radius, radius * 2, radius * 2);
        var text = new Rectangle((int)(27 * unit), 0, Width - (int)(32 * unit), Height);
        TextRenderer.DrawText(e.Graphics, Text, Font, text, ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) motion.Dispose();
        base.Dispose(disposing);
    }
}
