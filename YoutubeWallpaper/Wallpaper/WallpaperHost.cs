using Microsoft.Web.WebView2.Core;
using YoutubeWallpaper.App;
using YoutubeWallpaper.Playback;

namespace YoutubeWallpaper.Wallpaper;

internal enum PlaybackState { Inactive, Loading, Playing, Paused, Stopped, Error }
internal sealed record WallpaperStatus(PlaybackState State, string? Message = null);

// All lifecycle mutations run on the WinForms UI thread. Cancellation invalidates old continuations.
internal sealed class WallpaperHost : IDisposable
{
    private readonly string dataDirectory;
    private readonly LocalDiagnostics diagnostics;
    private readonly System.Windows.Forms.Timer monitor = new() { Interval = 1500 };
    private WallpaperForm? surface;
    private YouTubePlayer? player;
    private DesktopInterop.DesktopLayer? layer;
    private CancellationTokenSource? loading;
    private int generation;
    private bool disposed;
    private Rectangle screenBounds;
    private int desiredVolume, desiredIntensity;
    private uint desktopDpi;
    private DateTime loadingSince;
    public WallpaperStatus Status { get; private set; } = new(PlaybackState.Inactive);
    public YouTubeSource? Source { get; private set; }
    public bool IsActive => loading != null || surface != null;
    public event EventHandler<WallpaperStatus>? StatusChanged;

    public WallpaperHost(string dataDirectory)
    {
        this.dataDirectory = dataDirectory;
        diagnostics = new(dataDirectory);
        monitor.Tick += MonitorDesktop;
    }

    public async Task ApplyAsync(YouTubeSource source, int volume, int intensity)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ReleaseResources();
        int request = ++generation;
        var cancellation = new CancellationTokenSource();
        loading = cancellation;
        desiredVolume = Math.Clamp(volume, 0, 100);
        desiredIntensity = Math.Clamp(intensity, 0, 100);
        Source = source;
        SetStatus(PlaybackState.Loading);
        loadingSince = DateTime.UtcNow;
        monitor.Start();
        try
        {
            var target = await Task.Run(DesktopInterop.FindDesktopLayer, cancellation.Token);
            if (request != generation || disposed) return;
            cancellation.Token.ThrowIfCancellationRequested();
            layer = target;
            using (DesktopInterop.MatchDpiContext(target.Parent)) surface = new WallpaperForm();
            DesktopInterop.Attach(surface.SurfaceHandle, target);
            surface.Show();
            DesktopInterop.Resize(surface.SurfaceHandle, target);
            screenBounds = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;
            desktopDpi = DesktopInterop.GetDpiForWindow(target.Parent);
            player = new(surface.View);
            player.StateChanged += OnPlayerState;
            await player.InitializeAsync(source, desiredVolume, desiredIntensity, dataDirectory, cancellation.Token);
            if (request != generation || disposed) return;
            // "Reproduciendo" is only set by YouTube's actual playing event.
        }
        catch (Exception ex)
        {
            if (request != generation || disposed) return;
            diagnostics.Write("apply-failed", ex);
            string message = ex switch
            {
                WebView2RuntimeNotFoundException => "Falta Microsoft Edge WebView2 Runtime. Instala o repara el runtime Evergreen y vuelve a aplicar.",
                UnauthorizedAccessException => "No se puede acceder a los datos de WebView2. Comprueba los permisos de la carpeta de datos.",
                InvalidOperationException => ex.Message,
                System.ComponentModel.Win32Exception => "Windows rechazó el acoplamiento al escritorio. No ejecutes la aplicación como administrador. Consulta README.",
                _ => "No se pudo iniciar WebView2 o el wallpaper. Comprueba el runtime, la conexión y consulta diagnostics.log."
            };
            Fail(message);
        }
        finally
        {
            if (ReferenceEquals(loading, cancellation)) loading = null;
            cancellation.Dispose();
        }
    }

    public void SetVolume(int volume)
    {
        desiredVolume = Math.Clamp(volume, 0, 100);
        ControlPlayer(() => player?.SetVolume(desiredVolume));
    }
    public void SetIntensity(int intensity)
    {
        desiredIntensity = Math.Clamp(intensity, 0, 100);
        ControlPlayer(() => player?.SetIntensity(desiredIntensity));
    }
    public void Resume() => ControlPlayer(() =>
    {
        if (player == null) return;
        player.Resume();
        loadingSince = DateTime.UtcNow;
        SetStatus(PlaybackState.Loading);
    });
    private void ControlPlayer(Action action)
    {
        try { action(); }
        catch (Exception ex)
        {
            diagnostics.Write("player-command-failed", ex);
            Fail("El reproductor dejó de responder. Vuelve a aplicar el wallpaper.");
        }
    }

    private void OnPlayerState(object? sender, PlayerEvent e)
    {
        if (disposed || !ReferenceEquals(sender, player)) return;
        switch (e.Kind)
        {
            case "ready": break;
            case "playing": SetStatus(PlaybackState.Playing); break;
            case "paused": SetStatus(PlaybackState.Paused, "Reproducción pausada. Usa Reanudar desde la bandeja."); break;
            case "buffering":
                if (Status.State != PlaybackState.Loading) loadingSince = DateTime.UtcNow;
                SetStatus(PlaybackState.Loading); break;
            case "ended":
                // Playlist progression and single-video repeat belong to the official player.
                loadingSince = DateTime.UtcNow;
                SetStatus(PlaybackState.Loading); break;
            case "blocked": DeferFailure("El navegador o YouTube bloqueó el inicio automático. Prueba otro vídeo; consulta README."); break;
            case "error": DeferFailure(e.Detail ?? YouTubePlayer.ExplainError(e.Code)); break;
        }
    }

    private void DeferFailure(string message)
    {
        // Dispose outside WebView2's callback stack, and ignore callbacks for a replaced surface.
        int request = generation;
        if (surface is not { IsDisposed: false, IsHandleCreated: true } current) return;
        current.BeginInvoke((Action)(() => { if (request == generation && !disposed) Fail(message); }));
    }

    private void MonitorDesktop(object? sender, EventArgs e)
    {
        if (disposed) return;
        if (Status.State == PlaybackState.Loading && DateTime.UtcNow - loadingSince > TimeSpan.FromSeconds(45))
        {
            Fail("No se confirmó la reproducción en 45 segundos. Comprueba la conexión, el enlace y las restricciones de YouTube.");
            return;
        }
        if (surface == null || layer == null) return;
        try
        {
            if (!DesktopInterop.IsAttached(surface.SurfaceHandle, layer))
            {
                Fail("Explorer recreó o perdió la capa del escritorio. Se liberó el wallpaper. Vuelve a pulsar Aplicar.");
                return;
            }
            Rectangle bounds = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;
            uint dpi = DesktopInterop.GetDpiForWindow(layer.Parent);
            if (bounds != screenBounds || dpi != desktopDpi)
            {
                DesktopInterop.Resize(surface.SurfaceHandle, layer);
                screenBounds = bounds;
                desktopDpi = dpi;
            }
        }
        catch (Exception ex)
        {
            diagnostics.Write("desktop-monitor-failed", ex);
            Fail("La superficie del escritorio cambió y no pudo reajustarse. Vuelve a aplicar el wallpaper.");
        }
    }

    public void Stop()
    {
        ++generation;
        ReleaseResources();
        SetStatus(PlaybackState.Stopped);
    }
    private void Fail(string message)
    {
        ++generation;
        ReleaseResources();
        SetStatus(PlaybackState.Error, message);
        diagnostics.Write("wallpaper-stopped-after-error");
    }
    private void ReleaseResources()
    {
        monitor.Stop();
        var cancellation = loading;
        loading = null;
        cancellation?.Cancel();
        // The corresponding ApplyAsync finally disposes its CTS after initialization completes.
        var oldPlayer = player;
        var oldSurface = surface;
        player = null;
        surface = null;
        layer = null;
        Source = null;
        try { oldPlayer?.Dispose(); }
        catch (Exception ex) { diagnostics.Write("player-dispose-failed", ex); }
        finally
        {
            try { oldSurface?.Dispose(); }
            catch (Exception ex) { diagnostics.Write("surface-dispose-failed", ex); }
        }
    }
    private void SetStatus(PlaybackState state, string? message = null)
    {
        Status = new(state, message);
        StatusChanged?.Invoke(this, Status);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ++generation;
        ReleaseResources();
        monitor.Dispose();
        StatusChanged = null;
    }
}
