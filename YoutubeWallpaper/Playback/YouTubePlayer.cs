using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace YoutubeWallpaper.Playback;

internal sealed record PlayerEvent(string Kind, int Code = 0, string? Detail = null);

internal sealed class YouTubePlayer : IDisposable
{
    // A stable application identity, also declared in app.manifest; no remote server is used.
    internal const string LocalHost = "youtubewallpaper.local";
    private const string PlayerOrigin = "https://" + LocalHost;
    private readonly WebView2 view;
    private bool disposed;
    private int desiredVolume, desiredIntensity;
    public event EventHandler<PlayerEvent>? StateChanged;

    public YouTubePlayer(WebView2 view) => this.view = view;

    public async Task InitializeAsync(YouTubeSource source, int volume, int intensity, string dataDirectory, CancellationToken cancellation)
    {
        desiredVolume = Math.Clamp(volume, 0, 100);
        desiredIntensity = Math.Clamp(intensity, 0, 100);
        var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
        CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(
            null, Path.Combine(dataDirectory, "WebView2"), options);
        cancellation.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(disposed, this);
        await view.EnsureCoreWebView2Async(environment);
        cancellation.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(disposed, this);
        var core = view.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.IsSwipeNavigationEnabled = false;
        core.Settings.AreHostObjectsAllowed = false;
        core.SetVirtualHostNameToFolderMapping(LocalHost, Path.Combine(AppContext.BaseDirectory, "PlayerAssets"),
            CoreWebView2HostResourceAccessKind.DenyCors);
        core.WebMessageReceived += OnMessage;
        core.NavigationStarting += OnNavigationStarting;
        core.NavigationCompleted += OnNavigationCompleted;
        core.NewWindowRequested += OnNewWindow;
        core.PermissionRequested += OnPermission;
        core.ProcessFailed += OnProcessFailed;
        core.AddWebResourceRequestedFilter("https://www.youtube.com/embed/*", CoreWebView2WebResourceContext.Document);
        core.WebResourceRequested += OnEmbedRequest;

        string url = PlayerOrigin + "/player.html?v=" + (source.VideoId ?? "") +
            "&list=" + (source.PlaylistId ?? "") + "&index=" + source.PlaylistIndex +
            "&volume=" + desiredVolume + "&intensity=" + desiredIntensity;
        core.Navigate(url);
    }

    public void SetVolume(int volume)
    {
        desiredVolume = Math.Clamp(volume, 0, 100);
        Send(new { command = "volume", value = desiredVolume });
    }
    public void SetIntensity(int value)
    {
        desiredIntensity = Math.Clamp(value, 0, 100);
        Send(new { command = "intensity", value = desiredIntensity });
    }
    public void Resume() => Send(new { command = "resume" });

    private void Send(object command)
    {
        if (disposed || view.IsDisposed || view.CoreWebView2 == null) return;
        view.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(command));
    }

    private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (disposed || !IsLocalPage(e.Source)) return;
        try
        {
            using JsonDocument json = JsonDocument.Parse(e.WebMessageAsJson);
            string? kind = json.RootElement.GetProperty("kind").GetString();
            int code = json.RootElement.TryGetProperty("code", out var c) ? c.GetInt32() : 0;
            if (kind == "ready")
            {
                SetVolume(desiredVolume);
                SetIntensity(desiredIntensity);
            }
            if (kind is "ready" or "playing" or "paused" or "buffering" or "ended" or "error" or "blocked")
                StateChanged?.Invoke(this, new(kind, code));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            // Only structured messages from our local wrapper affect application state.
        }
    }

    private static bool IsLocalPage(string url) => Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
        uri.GetLeftPart(UriPartial.Authority) == PlayerOrigin && uri.AbsolutePath == "/player.html";

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!IsLocalPage(e.Uri)) e.Cancel = true; // Top-level navigation never leaves the packaged player.
    }
    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess && !disposed)
            StateChanged?.Invoke(this, new("error", 0, "No se pudo cargar el reproductor. Comprueba la conexión y WebView2."));
    }
    private static void OnEmbedRequest(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        // YouTube requires an embedder identity. Never impersonate youtube.com.
        e.Request.Headers.SetHeader("Referer", PlayerOrigin + "/");
    }
    private static void OnNewWindow(object? sender, CoreWebView2NewWindowRequestedEventArgs e) => e.Handled = true;
    private static void OnPermission(object? sender, CoreWebView2PermissionRequestedEventArgs e) => e.State = CoreWebView2PermissionState.Deny;
    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e) =>
        StateChanged?.Invoke(this, new("error", 0, "El proceso de WebView2 falló. Detén y vuelve a aplicar el wallpaper."));

    public static string ExplainError(int code) => code switch
    {
        2 => "YouTube rechazó los parámetros del vídeo o de la playlist.",
        5 => "YouTube no pudo reproducir este contenido en el reproductor HTML5.",
        100 => "El vídeo no existe, es privado o ya fue eliminado.",
        101 or 150 => "El propietario no permite insertar este vídeo. Prueba otro enlace.",
        153 => "YouTube rechazó la identificación de esta aplicación (error 153). Consulta README; no se elude esta restricción.",
        _ => "YouTube no pudo reproducir este contenido. Puede estar restringido por edad, autenticación, región o conexión."
    };

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (view.CoreWebView2 is { } core)
        {
            core.WebMessageReceived -= OnMessage;
            core.NavigationStarting -= OnNavigationStarting;
            core.NavigationCompleted -= OnNavigationCompleted;
            core.NewWindowRequested -= OnNewWindow;
            core.PermissionRequested -= OnPermission;
            core.ProcessFailed -= OnProcessFailed;
            core.WebResourceRequested -= OnEmbedRequest;
        }
        StateChanged = null;
        // The owning WallpaperForm disposes the WebView2 control, stopping all media.
    }
}
