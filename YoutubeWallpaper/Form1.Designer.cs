#nullable enable
namespace YoutubeWallpaper;

partial class Form1
{
    private System.ComponentModel.IContainer? components = null;
    private TextBox urlBox = null!;
    private TrackBar intensitySlider = null!;
    private TrackBar volumeSlider = null!;
    private Label intensityLabel = null!;
    private Label volumeLabel = null!;
    private Label statusLabel = null!;
    private Label errorLabel = null!;
    private Button applyButton = null!;
    private Button stopButton = null!;

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
        var background = Color.FromArgb(23, 28, 35);
        var muted = Color.FromArgb(162, 174, 189);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 14,
            BackColor = background
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int height in new[] { 44, 24, 38, 8, 24, 36, 28, 24, 36, 28, 52, 26, 72 })
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var title = new Label
        {
            Text = "YouTube Wallpaper", Dock = DockStyle.Fill, Margin = Padding.Empty,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(new Label { Text = "&URL de YouTube", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty }, 0, 1);
        urlBox = new TextBox
        {
            Dock = DockStyle.Fill, Margin = new Padding(0, 2, 0, 0), BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(37, 45, 56), ForeColor = Color.White, Font = new Font("Segoe UI", 10F),
            PlaceholderText = "https://www.youtube.com/watch?v=…", MaxLength = 2048,
            AccessibleName = "URL de YouTube", TabIndex = 0
        };
        layout.Controls.Add(urlBox, 0, 2);
        intensityLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty };
        intensitySlider = new TrackBar
        {
            Dock = DockStyle.Fill, Maximum = 100, Value = 85, TickStyle = TickStyle.None, LargeChange = 10,
            Margin = new Padding(0), BackColor = background, AccessibleName = "Intensidad visual", TabIndex = 1
        };
        layout.Controls.Add(intensityLabel, 0, 4);
        layout.Controls.Add(intensitySlider, 0, 5);
        layout.Controls.Add(new Label
        {
            Text = "Oscurece el vídeo; no deja ver el fondo estático.", Dock = DockStyle.Fill,
            ForeColor = muted, Font = new Font("Segoe UI", 8.5F), Margin = Padding.Empty
        }, 0, 6);
        volumeLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = Padding.Empty };
        volumeSlider = new TrackBar
        {
            Dock = DockStyle.Fill, Maximum = 100, Value = 20, TickStyle = TickStyle.None, LargeChange = 10,
            Margin = Padding.Empty, BackColor = background, AccessibleName = "Volumen", TabIndex = 2
        };
        layout.Controls.Add(volumeLabel, 0, 7);
        layout.Controls.Add(volumeSlider, 0, 8);
        layout.Controls.Add(new Label { Text = "Calidad: automática", ForeColor = muted, Dock = DockStyle.Fill, Margin = Padding.Empty }, 0, 9);
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        applyButton = new Button
        {
            Text = "&Aplicar wallpaper", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 10),
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(126, 200, 250), ForeColor = Color.FromArgb(15, 30, 45),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold), UseVisualStyleBackColor = false, TabIndex = 3
        };
        applyButton.FlatAppearance.BorderSize = 0;
        stopButton = new Button
        {
            Text = "&Detener", Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10),
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(43, 53, 66), ForeColor = Color.White,
            UseVisualStyleBackColor = false, Enabled = false, TabIndex = 4
        };
        stopButton.FlatAppearance.BorderColor = Color.FromArgb(78, 94, 114);
        actions.Controls.Add(applyButton, 0, 0);
        actions.Controls.Add(stopButton, 1, 0);
        layout.Controls.Add(actions, 0, 10);
        statusLabel = new Label { Dock = DockStyle.Fill, Text = "●  Inactivo", Margin = Padding.Empty, TextAlign = ContentAlignment.MiddleLeft };
        errorLabel = new Label { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(255, 165, 150), Margin = Padding.Empty };
        layout.Controls.Add(statusLabel, 0, 11);
        layout.Controls.Add(errorLabel, 0, 12);
        layout.Controls.Add(new Label
        {
            Text = "Cerrar esta ventana deja la app en la bandeja.\nPara terminar, elige Salir en el icono junto al reloj.",
            ForeColor = muted, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5F), Margin = Padding.Empty
        }, 0, 13);
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9F);
        BackColor = background;
        ForeColor = Color.FromArgb(236, 241, 247);
        ClientSize = new Size(500, 548);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "YouTube Wallpaper";
        Name = "Form1";
        AcceptButton = applyButton;
        Controls.Add(layout);
        ResumeLayout(false);
    }
}
