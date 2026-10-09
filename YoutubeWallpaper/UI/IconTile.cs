using System.Drawing.Drawing2D;

namespace YoutubeWallpaper.UI;

internal enum TileIcon { Play, Sun, Volume, Tray }

internal sealed class IconTile : Control
{
    public TileIcon Symbol { get; set; }
    public Color Accent { get; set; } = Theme.Violet;
    public bool Filled { get; set; } = true;
    public IconTile()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        TabStop = false;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        Theme.Prepare(e.Graphics);
        float side = Math.Min(Width, Height) - Theme.Scale(this, 4);
        if (side <= 0) return;
        var bounds = new RectangleF((Width - side) / 2, (Height - side) / 2, side, side);
        if (Filled)
        {
            using var shape = Theme.Round(bounds, side * 0.3f);
            using var brush = new LinearGradientBrush(bounds, Color.FromArgb(145, Accent),
                Color.FromArgb(45, Accent), 50);
            e.Graphics.FillPath(brush, shape);
            using var outline = new Pen(Color.FromArgb(90, Accent), Theme.Scale(this, 1));
            e.Graphics.DrawPath(outline, shape);
        }
        GraphicsState state = e.Graphics.Save();
        e.Graphics.TranslateTransform(bounds.Left, bounds.Top);
        e.Graphics.ScaleTransform(side / 40, side / 40);
        using var pen = new Pen(Filled ? Theme.Text : Accent, 1.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        switch (Symbol)
        {
            case TileIcon.Play:
                e.Graphics.DrawPolygon(pen, new[] { new PointF(16, 12), new PointF(28, 20), new PointF(16, 28) });
                break;
            case TileIcon.Sun:
                e.Graphics.DrawEllipse(pen, 15, 15, 10, 10);
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4;
                    e.Graphics.DrawLine(pen, 20 + (float)Math.Cos(angle) * 9, 20 + (float)Math.Sin(angle) * 9,
                        20 + (float)Math.Cos(angle) * 12, 20 + (float)Math.Sin(angle) * 12);
                }
                break;
            case TileIcon.Volume:
                e.Graphics.DrawPolygon(pen, new[] { new PointF(10, 17), new PointF(15, 17), new PointF(21, 12),
                    new PointF(21, 28), new PointF(15, 23), new PointF(10, 23) });
                e.Graphics.DrawArc(pen, 18, 13, 11, 14, -65, 130);
                e.Graphics.DrawArc(pen, 18, 9, 18, 22, -60, 120);
                break;
            case TileIcon.Tray:
                e.Graphics.DrawLine(pen, 11, 15, 11, 28);
                e.Graphics.DrawLine(pen, 11, 28, 29, 28);
                e.Graphics.DrawLine(pen, 29, 28, 29, 15);
                e.Graphics.DrawLine(pen, 20, 10, 20, 23);
                e.Graphics.DrawLine(pen, 15, 18, 20, 23);
                e.Graphics.DrawLine(pen, 20, 23, 25, 18);
                break;
        }
        e.Graphics.Restore(state);
    }
}
