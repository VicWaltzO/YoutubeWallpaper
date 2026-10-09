using YoutubeWallpaper.App;
using YoutubeWallpaper.Playback;
using YoutubeWallpaper.Wallpaper;
using YoutubeWallpaper.UI;

namespace YoutubeWallpaper;

public partial class Form1 : Form
{
    private readonly SettingsStore settingsStore;
    private readonly WallpaperHost wallpaper;
    private readonly TrayController tray;
    private readonly System.Windows.Forms.Timer saveTimer = new() { Interval = 500 };
    private bool exiting, resourcesReleased;
    private string savedUrl;
    private string? notice;

    public Form1() : this(new SettingsStore()) { }

    internal Form1(SettingsStore settingsStore)
    {
        this.settingsStore = settingsStore;
        InitializeComponent();
        Icon = AppIcon.Load();
        AppSettings settings = settingsStore.Load(out notice);
        savedUrl = settings.LastUrl;
        urlBox.Text = settings.LastUrl;
        volumeSlider.Value = settings.Volume;
        intensitySlider.Value = settings.Opacity;
        wallpaper = new(settingsStore.DirectoryPath);
        tray = new(ShowConfiguration, ApplyOrResume, wallpaper.Stop, ExitApplication);
        wallpaper.StatusChanged += (_, status) =>
        {
            if (status.State == PlaybackState.Error) notice = null;
            RefreshStatus();
        };
        applyButton.Click += async (_, _) => await ApplyWallpaperAsync();
        stopButton.Click += (_, _) => wallpaper.Stop();
        urlBox.TextChanged += (_, _) => RefreshStatus();
        volumeSlider.ValueChanged += (_, _) =>
        {
            UpdateValues();
            wallpaper.SetVolume(volumeSlider.Value);
            QueueSave();
        };
        intensitySlider.ValueChanged += (_, _) =>
        {
            UpdateValues();
            wallpaper.SetIntensity(intensitySlider.Value);
            QueueSave();
        };
        saveTimer.Tick += (_, _) => { saveTimer.Stop(); SaveSettings(); };
        UpdateValues();
        RefreshStatus();
    }

    private async Task ApplyWallpaperAsync()
    {
        if (exiting) return;
        if (!YouTubeUrlParser.TryParse(urlBox.Text, out YouTubeSource? source, out string error))
        {
            notice = error;
            RefreshStatus();
            urlBox.Focus();
            return;
        }
        savedUrl = source!.CanonicalUrl;
        urlBox.Text = savedUrl;
        notice = null;
        SaveSettings();
        await wallpaper.ApplyAsync(source, volumeSlider.Value, intensitySlider.Value);
    }

    private async void ApplyOrResume()
    {
        if (wallpaper.Status.State == PlaybackState.Paused &&
            YouTubeUrlParser.TryParse(urlBox.Text, out var source, out _) &&
            source == wallpaper.Source)
            wallpaper.Resume();
        else await ApplyWallpaperAsync();
    }

    private void UpdateValues()
    {
        volumeLabel.Text = $"{volumeSlider.Value} %";
        intensityLabel.Text = $"{intensitySlider.Value} %";
    }
    private void QueueSave() { saveTimer.Stop(); saveTimer.Start(); }
    private void SaveSettings()
    {
        if (!settingsStore.Save(new() { LastUrl = savedUrl, Volume = volumeSlider.Value, Opacity = intensitySlider.Value }, out string? error))
        {
            notice = error;
            RefreshStatus();
        }
    }
    private void RefreshStatus()
    {
        if (resourcesReleased) return;
        WallpaperStatus status = wallpaper.Status;
        bool valid = YouTubeUrlParser.TryParse(urlBox.Text, out var pendingSource, out _);
        applyButton.Enabled = status.State != PlaybackState.Loading && !exiting;
        stopButton.Enabled = wallpaper.IsActive && !exiting;
        statusLabel.Text = TrayController.StatusText(status.State);
        bool loading = status.State == PlaybackState.Loading;
        applyButton.IsLoading = loading;
        statusLabel.IsLoading = loading;
        statusLabel.ForeColor = status.State switch
        {
            PlaybackState.Playing => Theme.Green,
            PlaybackState.Error => Theme.Error,
            _ => Theme.Cyan
        };
        errorLabel.Text = notice ?? status.Message ?? "";
        tray.Update(status, wallpaper.IsActive, valid &&
            (status.State != PlaybackState.Playing || pendingSource != wallpaper.Source));
    }

    public void ShowConfiguration()
    {
        if (exiting || IsDisposed) return;
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
        RefreshStatus();
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowAppearance.Apply(Handle);
    }
    private void ExitApplication()
    {
        if (exiting) return;
        exiting = true;
        SaveSettings();
        ReleaseApplicationResources();
        Close();
        Application.Exit();
    }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!exiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            SaveSettings();
            Hide();
            ShowInTaskbar = false;
            tray.ExplainHideOnce();
        }
        else
        {
            exiting = true;
            SaveSettings();
            ReleaseApplicationResources();
        }
        base.OnFormClosing(e);
    }
    private void ReleaseApplicationResources()
    {
        if (resourcesReleased) return;
        resourcesReleased = true;
        saveTimer.Stop();
        saveTimer.Dispose();
        wallpaper?.Dispose();
        tray?.Dispose();
        Icon?.Dispose();
    }
}
