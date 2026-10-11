#nullable enable
using YoutubeWallpaper.UI;

namespace YoutubeWallpaper;

partial class Form1
{
    private System.ComponentModel.IContainer? components = null;
    private TextBox urlBox = null!;
    private AnimatedSlider intensitySlider = null!;
    private AnimatedSlider volumeSlider = null!;
    private StatusBadge statusLabel = null!;
    private Label errorLabel = null!;
    private AnimatedButton applyButton = null!;
    private AnimatedButton stopButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseApplicationResources();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();
        var frame = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16), BackColor = Theme.Canvas };
        var shell = new SurfacePanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(24), CornerRadius = 24,
            TopColor = Color.FromArgb(43, 59, 70), BottomColor = Color.FromArgb(28, 41, 51)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, BackColor = Color.Transparent, Margin = Padding.Empty,
            ColumnCount = 1, RowCount = 11
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int height in new[] { 60, 8, 128, 12, 158, 12, 24, 8, 54, 52 })
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, ColumnCount = 3, RowCount = 2, Margin = Padding.Empty };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var logo = new IconTile { Symbol = TileIcon.Play, Accent = Theme.Violet, Dock = DockStyle.Fill, Margin = new Padding(0, 3, 8, 8) };
        header.Controls.Add(logo, 0, 0);
        header.SetRowSpan(logo, 2);
        header.Controls.Add(Caption("YouTube Wallpaper", 18.5F, FontStyle.Bold, Theme.Text), 1, 0);
        header.Controls.Add(Caption("Tu escritorio, en movimiento.", 9F, FontStyle.Regular, Theme.Muted), 1, 1);
        statusLabel = new StatusBadge
        {
            Text = "Inactivo", ForeColor = Theme.Cyan, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Size = new Size(124, 32), Anchor = AnchorStyles.Right | AnchorStyles.Top, Margin = new Padding(6, 9, 0, 0)
        };
        header.Controls.Add(statusLabel, 2, 0);
        header.SetRowSpan(statusLabel, 2);
        layout.Controls.Add(header, 0, 0);

        var sourceCard = new SurfacePanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(20, 14, 20, 12), Margin = Padding.Empty,
            TopColor = Color.FromArgb(55, 75, 89), BottomColor = Color.FromArgb(40, 57, 70), CornerRadius = 17
        };
        var sourceLayout = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
        sourceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sourceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        sourceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        sourceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sourceLayout.Controls.Add(Caption("&URL de YouTube", 10F, FontStyle.Bold, Theme.Text), 0, 0);
        var input = new SurfacePanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(14, 12, 14, 8), Margin = Padding.Empty,
            TopColor = Color.FromArgb(30, 46, 59), BottomColor = Color.FromArgb(30, 46, 59),
            CornerRadius = 10, HasShadow = false, Accent = Theme.Violet
        };
        urlBox = new TextBox
        {
            Dock = DockStyle.Fill, Margin = Padding.Empty, BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(30, 46, 59), ForeColor = Theme.Text, Font = new Font("Segoe UI", 10F),
            PlaceholderText = "Pega un enlace de vídeo o playlist…", MaxLength = 2048,
            AccessibleName = "URL de YouTube", TabIndex = 0
        };
        urlBox.Enter += (_, _) => input.Highlighted = true;
        urlBox.Leave += (_, _) => input.Highlighted = false;
        input.Controls.Add(urlBox);
        sourceLayout.Controls.Add(input, 0, 1);
        sourceLayout.Controls.Add(Caption("Vídeos y playlists · reproducción integrada", 8.5F, FontStyle.Regular, Theme.Muted), 0, 2);
        sourceCard.Controls.Add(sourceLayout);
        layout.Controls.Add(sourceCard, 0, 2);

        var sliders = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        sliders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        sliders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        intensitySlider = new AnimatedSlider
        {
            Dock = DockStyle.Fill, Maximum = 100, Value = 85, LargeChange = 10, Accent = Theme.Violet,
            ShowValue = true, Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            Margin = Padding.Empty, AccessibleName = "Intensidad visual", TabIndex = 1
        };
        volumeSlider = new AnimatedSlider
        {
            Dock = DockStyle.Fill, Maximum = 100, Value = 20, LargeChange = 10, Accent = Theme.Cyan,
            ShowValue = true, Font = new Font("Segoe UI", 24F, FontStyle.Bold),
            Margin = Padding.Empty, AccessibleName = "Volumen", TabIndex = 2
        };
        var intensityCard = SliderCard("Intensidad visual", intensitySlider, TileIcon.Sun,
            Theme.Violet, "Oscurece el vídeo.\nNo es transparencia.");
        intensityCard.Margin = new Padding(0, 0, 6, 0);
        var volumeCard = SliderCard("Volumen", volumeSlider, TileIcon.Volume,
            Theme.Cyan, "Silencio al llegar a 0 %.");
        volumeCard.Margin = new Padding(6, 0, 0, 0);
        sliders.Controls.Add(intensityCard, 0, 0);
        sliders.Controls.Add(volumeCard, 1, 0);
        layout.Controls.Add(sliders, 0, 4);
        var quality = Caption("Calidad de vídeo  ·  Automática", 9F, FontStyle.Regular, Theme.Muted);
        layout.Controls.Add(quality, 0, 6);

        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        applyButton = new AnimatedButton
        {
            Text = "&Aplicar wallpaper", Primary = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0),
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), TabIndex = 3
        };
        stopButton = new AnimatedButton
        {
            Text = "&Detener", Dock = DockStyle.Fill, Margin = Padding.Empty,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold), Enabled = false, TabIndex = 4
        };
        actions.Controls.Add(applyButton, 0, 0);
        actions.Controls.Add(stopButton, 1, 0);
        layout.Controls.Add(actions, 0, 8);
        errorLabel = Caption("", 8.5F, FontStyle.Regular, Theme.Error);
        errorLabel.Margin = new Padding(4, 8, 4, 0);
        errorLabel.TextAlign = ContentAlignment.TopLeft;
        layout.Controls.Add(errorLabel, 0, 9);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 21));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var trayIcon = new IconTile { Symbol = TileIcon.Tray, Accent = Theme.Muted, Filled = false, Dock = DockStyle.Fill, Margin = Padding.Empty };
        footer.Controls.Add(trayIcon, 0, 0);
        footer.SetRowSpan(trayIcon, 2);
        footer.Controls.Add(Caption("Control desde la bandeja", 9F, FontStyle.Bold, Theme.Muted), 1, 0);
        footer.Controls.Add(Caption("X oculta el panel · Salir cierra la aplicación", 8.5F, FontStyle.Regular, Theme.Muted), 1, 1);
        layout.Controls.Add(footer, 0, 10);

        shell.Controls.Add(layout);
        frame.Controls.Add(shell);
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9F);
        BackColor = Theme.Canvas;
        ForeColor = Theme.Text;
        ClientSize = new Size(640, 640);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "YouTube Wallpaper";
        Name = "Form1";
        AcceptButton = applyButton;
        Controls.Add(frame);
        ResumeLayout(false);
    }

    private static Label Caption(string text, float size, FontStyle style, Color color) => new()
    {
        Text = text, Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.Transparent,
        Font = new Font("Segoe UI", size, style), ForeColor = color, TextAlign = ContentAlignment.MiddleLeft
    };

    private static SurfacePanel SliderCard(string title, AnimatedSlider slider, TileIcon symbol, Color accent, string hint)
    {
        var card = new SurfacePanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(18, 12, 18, 12), CornerRadius = 17,
            TopColor = Color.FromArgb(51, 70, 84), BottomColor = Color.FromArgb(36, 52, 65)
        };
        var rows = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.Transparent, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty };
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28));
        foreach (int height in new[] { 25, 81 })
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rows.Controls.Add(Caption(title, 10F, FontStyle.Bold, Theme.Text), 0, 0);
        rows.Controls.Add(new IconTile { Symbol = symbol, Accent = accent, Filled = false, Dock = DockStyle.Fill, Margin = Padding.Empty }, 1, 0);
        rows.Controls.Add(slider, 0, 1);
        rows.SetColumnSpan(slider, 2);
        var description = Caption(hint, 8.5F, FontStyle.Regular, Theme.Muted);
        rows.Controls.Add(description, 0, 2);
        rows.SetColumnSpan(description, 2);
        card.Controls.Add(rows);
        return card;
    }
}
