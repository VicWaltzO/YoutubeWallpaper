using YoutubeWallpaper.Wallpaper;

namespace YoutubeWallpaper.App;

internal sealed class TrayController : IDisposable
{
    private readonly NotifyIcon icon;
    private readonly Icon image = AppIcon.Load();
    private readonly ContextMenuStrip menu = new();
    private readonly ToolStripMenuItem apply;
    private readonly ToolStripMenuItem stop;
    private bool explainedHide;

    public TrayController(Action open, Action applyOrResume, Action stopWallpaper, Action exit)
    {
        menu.Items.Add("Abrir configuración", null, (_, _) => open());
        apply = new("Aplicar / Reanudar", null, (_, _) => applyOrResume());
        stop = new("Detener wallpaper", null, (_, _) => stopWallpaper());
        menu.Items.Add(apply);
        menu.Items.Add(stop);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => exit());
        icon = new NotifyIcon { Icon = image, Text = "YouTube Wallpaper · Inactivo", ContextMenuStrip = menu, Visible = true };
        icon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) open(); };
        icon.DoubleClick += (_, _) => open();
    }

    public void Update(WallpaperStatus status, bool active, bool canApply)
    {
        apply.Enabled = canApply && status.State != PlaybackState.Loading;
        apply.Text = status.State == PlaybackState.Paused ? "Reanudar" : "Aplicar wallpaper";
        stop.Enabled = active;
        icon.Text = "YouTube Wallpaper · " + StatusText(status.State);
    }
    internal static string StatusText(PlaybackState state) => state switch
    {
        PlaybackState.Loading => "Cargando",
        PlaybackState.Playing => "Reproduciendo",
        PlaybackState.Paused => "Pausado",
        PlaybackState.Stopped => "Detenido",
        PlaybackState.Error => "Error",
        _ => "Inactivo"
    };
    public void ExplainHideOnce()
    {
        if (explainedHide) return;
        explainedHide = true;
        icon.ShowBalloonTip(3500, "YouTube Wallpaper sigue disponible",
            "Abre los ajustes desde el icono junto al reloj o en ^. Para cerrar la aplicación, elige Salir.", ToolTipIcon.Info);
    }
    public void Dispose()
    {
        icon.Visible = false;
        icon.Dispose();
        menu.Dispose();
        image.Dispose();
    }
}
