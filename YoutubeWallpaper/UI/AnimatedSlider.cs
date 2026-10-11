using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace YoutubeWallpaper.UI;

internal class AnimatedSlider : Control
{
    private readonly Motion motion;
    private int value, maximum = 100;
    private float position, hover;
    private bool hovered;
    public Color Accent { get; set; } = Theme.Cyan;
    [DefaultValue(100)]
    public int Maximum
    {
        get => maximum;
        set
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));
            maximum = value;
            Value = Math.Min(this.value, value);
            motion.Request();
        }
    }
    [DefaultValue(0)]
    public int Value
    {
        get => value;
        set
        {
            if (value < 0 || value > maximum) throw new ArgumentOutOfRangeException(nameof(value));
            if (this.value == value) return;
            this.value = value;
            // Dragging is direct manipulation; easing belongs to click/keyboard/programmatic changes.
            if (Capture) position = (float)value / maximum;
            motion.Request();
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    [DefaultValue(10)] public int LargeChange { get; set; } = 10;
    public event EventHandler? ValueChanged;
    internal bool IsAnimating => motion.IsRunning;
    internal float VisualPosition => position;

    public AnimatedSlider()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor | ControlStyles.Selectable, true);
        BackColor = Color.Transparent;
        AccessibleRole = AccessibleRole.Slider;
        TabStop = true;
        Cursor = Cursors.Hand;
        Size = new Size(240, 38);
        motion = new(this, elapsed =>
        {
            float target = (float)value / maximum, hoverTarget = hovered || Capture ? 1 : 0;
            position = Motion.Approach(position, target, elapsed);
            hover = Motion.Approach(hover, hoverTarget, elapsed);
            return position != target || hover != hoverTarget;
        });
    }
    protected override bool IsInputKey(Keys keyData) => (keyData & Keys.KeyCode) is
        Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        int next = e.KeyCode switch
        {
            Keys.Left or Keys.Down => value - 1,
            Keys.Right or Keys.Up => value + 1,
            Keys.PageDown => value - LargeChange,
            Keys.PageUp => value + LargeChange,
            Keys.Home => 0,
            Keys.End => maximum,
            _ => value
        };
        if (IsInputKey(e.KeyCode))
        {
            Value = Math.Clamp(next, 0, maximum);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        base.OnKeyDown(e);
    }
    private void SetFromPointer(int x)
    {
        float inset = Theme.Scale(this, 12);
        Value = (int)Math.Round(Math.Clamp((x - inset) / Math.Max(1, Width - inset * 2), 0, 1) * maximum);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !Enabled) return;
        Focus();
        Capture = true;
        SetFromPointer(e.X);
        position = (float)value / maximum;
        motion.Request();
    }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (Capture) SetFromPointer(e.X); }
    protected override void OnMouseUp(MouseEventArgs e) { Capture = false; base.OnMouseUp(e); motion.Request(); }
    protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); motion.Request(); }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; motion.Request(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; motion.Request(); }
    protected override void OnVisibleChanged(EventArgs e)
    {
        if (!Visible) { hovered = false; Capture = false; }
        base.OnVisibleChanged(e);
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (Focused) Value = Math.Clamp(value + Math.Sign(e.Delta), 0, maximum);
        base.OnMouseWheel(e);
    }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        Theme.Prepare(e.Graphics);
        float unit = DeviceDpi / 96f, inset = 12 * unit, y = Height / 2f, width = Math.Max(1, Width - inset * 2);
        using var rail = new Pen(Color.FromArgb(27, 41, 52), 5 * unit) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawLine(rail, inset, y, Width - inset, y);
        float x = inset + width * position;
        if (position > 0)
        {
            using var gradient = new LinearGradientBrush(new RectangleF(inset, y - 3 * unit, width, 6 * unit),
                Theme.Mix(Accent, Color.White, 0.12f), Accent, 0f);
            using var active = new Pen(gradient, 5 * unit) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            e.Graphics.DrawLine(active, inset, y, x, y);
        }
        float radius = (7 + hover * 1.8f) * unit;
        using var halo = new SolidBrush(Color.FromArgb((int)(22 + hover * 24), Accent));
        e.Graphics.FillEllipse(halo, x - radius - 5 * unit, y - radius - 5 * unit, (radius + 5 * unit) * 2, (radius + 5 * unit) * 2);
        using var thumb = new SolidBrush(Theme.Mix(Accent, Color.White, 0.75f));
        e.Graphics.FillEllipse(thumb, x - radius, y - radius, radius * 2, radius * 2);
        using var dot = new SolidBrush(Accent);
        e.Graphics.FillEllipse(dot, x - 2.2f * unit, y - 2.2f * unit, 4.4f * unit, 4.4f * unit);
        if (Focused && ShowFocusCues)
        {
            using var focusPen = new Pen(Accent, unit) { DashStyle = DashStyle.Dot };
            e.Graphics.DrawEllipse(focusPen, x - radius - 3 * unit, y - radius - 3 * unit, (radius + 3 * unit) * 2, (radius + 3 * unit) * 2);
        }
    }
    protected override AccessibleObject CreateAccessibilityInstance() => new SliderAccessibleObject(this);
    private sealed class SliderAccessibleObject(AnimatedSlider owner) : ControlAccessibleObject(owner)
    {
        public override AccessibleRole Role => AccessibleRole.Slider;
        public override string? Value
        {
            get => owner.Value.ToString(System.Globalization.CultureInfo.CurrentCulture);
            set { if (int.TryParse(value, out int parsed)) owner.Value = Math.Clamp(parsed, 0, owner.Maximum); }
        }
        public override string? Description => owner.AccessibleDescription ?? $"De 0 a {owner.Maximum} por ciento. Usa las flechas para ajustar.";
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) motion.Dispose();
        base.Dispose(disposing);
    }
}
