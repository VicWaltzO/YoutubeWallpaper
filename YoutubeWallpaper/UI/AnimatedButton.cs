using System.Drawing.Drawing2D;

namespace YoutubeWallpaper.UI;

internal sealed class AnimatedButton : Button
{
    private readonly Motion motion;
    private bool hovered, pressed, loading;
    private float hover, press, rotation;
    public bool Primary { get; set; }
    public bool IsLoading
    {
        get => loading;
        set { if (loading == value) return; loading = value; motion.Request(); }
    }
    internal bool IsAnimating => motion.IsRunning;

    public AnimatedButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        motion = new(this, elapsed =>
        {
            float hoverTarget = hovered && Enabled ? 1 : 0, pressTarget = pressed && Enabled ? 1 : 0;
            hover = Motion.Approach(hover, hoverTarget, elapsed);
            press = Motion.Approach(press, pressTarget, elapsed);
            if (loading && !float.IsPositiveInfinity(elapsed)) rotation = (rotation + elapsed * 220) % 360;
            return hover != hoverTarget || press != pressTarget || loading;
        });
    }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; motion.Request(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = pressed = false; motion.Request(); }
    protected override void OnVisibleChanged(EventArgs e)
    {
        if (!Visible) hovered = pressed = false;
        base.OnVisibleChanged(e);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left) { pressed = true; motion.Request(); }
    }
    protected override void OnMouseUp(MouseEventArgs e) { pressed = false; motion.Request(); base.OnMouseUp(e); }
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        if (!Capture) { pressed = false; motion.Request(); }
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Space or Keys.Enter) { pressed = true; motion.Request(); }
        base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e) { pressed = false; motion.Request(); base.OnKeyUp(e); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); motion.Request(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        Theme.Prepare(e.Graphics);
        float unit = DeviceDpi / 96f;
        var bounds = new RectangleF(2 * unit + press * unit, (2 + press) * unit,
            Width - (5 + press * 2) * unit, Height - (6 + press * 2) * unit);
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var path = Theme.Round(bounds, 13 * unit);
        Color start = Primary ? Theme.Mix(Color.FromArgb(151, 142, 230), Color.FromArgb(181, 165, 252), hover) :
            Theme.Mix(Color.FromArgb(53, 70, 83), Color.FromArgb(68, 89, 103), hover);
        Color end = Primary ? Theme.Mix(Color.FromArgb(107, 198, 215), Color.FromArgb(141, 224, 236), hover) :
            Color.FromArgb(40, 56, 68);
        if (!Enabled && !loading) { start = Color.FromArgb(39, 52, 62); end = Color.FromArgb(33, 46, 56); }
        using var fill = new LinearGradientBrush(bounds, start, end, 15);
        e.Graphics.FillPath(fill, path);
        using var border = new Pen(Color.FromArgb(Primary ? 90 : 125, 125, 154, 175), unit);
        e.Graphics.DrawPath(border, path);
        Color ink = Primary ? Color.FromArgb(22, 36, 51) : Enabled ? Theme.Text : Theme.Muted;
        Rectangle textBounds = Rectangle.Round(bounds);
        if (Primary)
        {
            float x = bounds.Left + 24 * unit + hover * 2 * unit, y = bounds.Top + bounds.Height / 2;
            using var pen = new Pen(ink, 1.8f * unit) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            if (loading) e.Graphics.DrawArc(pen, x - 3 * unit, y - 7 * unit, 14 * unit, 14 * unit, rotation, 255);
            else
            {
                using var triangle = new GraphicsPath();
                triangle.AddPolygon(new[] { new PointF(x, y - 5 * unit), new PointF(x + 8 * unit, y), new PointF(x, y + 5 * unit) });
                e.Graphics.DrawPath(pen, triangle);
            }
            textBounds.X += (int)(18 * unit);
            textBounds.Width -= (int)(18 * unit);
        }
        TextRenderer.DrawText(e.Graphics, loading ? "Cargando…" : Text, Font, textBounds, ink,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            (ShowKeyboardCues ? 0 : TextFormatFlags.HidePrefix));
        if (Focused && ShowFocusCues)
        {
            var ring = bounds;
            ring.Inflate(-4 * unit, -4 * unit);
            using var focusPath = Theme.Round(ring, 9 * unit);
            using var focusPen = new Pen(Primary ? Color.FromArgb(25, 50, 65) : Theme.Cyan, unit) { DashStyle = DashStyle.Dot };
            e.Graphics.DrawPath(focusPen, focusPath);
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) motion.Dispose();
        base.Dispose(disposing);
    }
}
