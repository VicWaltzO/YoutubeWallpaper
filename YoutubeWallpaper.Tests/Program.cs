using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using YoutubeWallpaper.App;
using YoutubeWallpaper.Playback;
using YoutubeWallpaper.Wallpaper;
using YoutubeWallpaper.UI;

namespace YoutubeWallpaper.Tests;

internal static class Program
{
    private static int failures, assertions;
    private static bool captureMotion;
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            captureMotion = args.Contains("--motion");
            TestUrls();
            TestSettings();
            if (args.Contains("--ui")) RunMessageLoop(TestPanelAsync);
            if (args.Contains("--desktop")) RunMessageLoop(TestDesktopAsync);
            int playback = Array.IndexOf(args, "--playback");
            if (playback >= 0 && args.Length > playback + 1)
                RunMessageLoop(() => TestPlaybackAsync(args[playback + 1]));
        }
        catch (Exception ex) { failures++; Console.WriteLine("FAIL: " + ex); }
        Console.WriteLine($"Resultado: {assertions} comprobaciones, {failures} fallos.");
        return failures == 0 ? 0 : 1;
    }
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) { failures++; Console.WriteLine("FAIL: " + message); }
    }

    private static void TestUrls()
    {
        int previousFailures = failures;
        const string id = "M7lc1UVf-VE", list = "PL1234567890abcdef";
        var valid = new[]
        {
            ("https://www.youtube.com/watch?v=" + id, id, (string?)null),
            ("https://youtu.be/" + id + "?si=private-tracking", id, (string?)null),
            ("https://www.youtube.com/playlist?list=" + list, (string?)null, list),
            ("https://www.youtube.com/watch?v=" + id + "&list=" + list + "&index=3", id, list),
            ("https://m.youtube.com/watch?v=" + id, id, (string?)null)
        };
        foreach (var (url, video, playlist) in valid)
        {
            bool parsed = YouTubeUrlParser.TryParse(url, out var source, out _);
            Check(parsed && source?.VideoId == video && source?.PlaylistId == playlist, "Parser: " + url);
            Check(source != null && !source.CanonicalUrl.Contains("private-tracking"), "No persiste seguimiento");
        }
        Check(YouTubeUrlParser.TryParse(valid[3].Item1, out var indexed, out _) && indexed?.PlaylistIndex == 2, "Índice de playlist");
        foreach (string url in new[]
        {
            "", "javascript:alert(1)", "file:///C:/video.html", "http://www.youtube.com/watch?v=" + id,
            "https://localhost/watch?v=" + id, "https://www.youtube.com.evil.example/watch?v=" + id,
            "https://youtube.com@evil.example/watch?v=" + id, "https://user@youtube.com/watch?v=" + id,
            "https://www.youtube.com:444/watch?v=" + id, "https://youtu.be/" + id + "/extra",
            "https://www.youtube.com/watch?v=short", "https://www.youtube.com/watch?v=" + id + "&v=other",
            "https://www.youtube.com/playlist", "https://www.youtube.com/playlist?list=<script>",
            "https://www.youtube.com/watch?v=" + id + "&list=" + list + "&index=-1",
            "https://www.youtube.com/redirect?q=https://evil.example", "https://www.youtube.com/watch?v=%22onclick%3D1"
        })
            Check(!YouTubeUrlParser.TryParse(url, out _, out _), "Rechaza URL no admitida");
        Report(previousFailures, "parser, normalización y rechazo de URL insegura.");
    }
    private static string TestDirectory(string name)
    {
        string path = Path.Combine(Environment.CurrentDirectory, ".build", "tests", name + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
    private static void TestSettings()
    {
        int previousFailures = failures;
        string path = TestDirectory("settings");
        var store = new SettingsStore(path);
        Check(store.Load(out var notice) == new AppSettings() && notice == null, "Defaults sin archivo");
        Check(store.Save(new() { LastUrl = "https://youtu.be/M7lc1UVf-VE?si=secret", Volume = 0, Opacity = 50 }, out _), "Guardar ajustes");
        var loaded = store.Load(out notice);
        Check(loaded.Volume == 0 && loaded.Opacity == 50 && loaded.LastUrl == "https://www.youtube.com/watch?v=M7lc1UVf-VE", "Roundtrip y URL canónica");
        Check(!File.ReadAllText(store.FilePath).Contains("secret"), "JSON sin seguimiento");
        for (int n = 0; n < 20; n++)
        {
            bool saved = store.Save(new() { Volume = n }, out var writeError);
            var current = store.Load(out var readNotice);
            Check(saved && current.Volume == n, $"Reemplazo atómico {n}: guardado={saved}, leído={current.Volume}; {writeError ?? readNotice}");
        }
        Check(!Directory.EnumerateFiles(path, "*.tmp").Any(), "Sin temporales huérfanos");
        File.WriteAllText(store.FilePath, "{broken");
        Check(store.Load(out notice) == new AppSettings() && notice != null, "JSON corrupto recuperable");
        File.WriteAllText(store.FilePath, """{"schemaVersion":1,"volume":120,"opacity":-1,"qualityMode":"1080p","lastUrl":"file:///secret"}""");
        Check(store.Load(out _) == new AppSettings(), "Valores fuera de rango recuperables");
        File.WriteAllText(store.FilePath, """{"schemaVersion":999,"volume":0}""");
        Check(store.Load(out notice) == new AppSettings() && notice != null, "Versión desconocida recuperable");
        using (File.Open(store.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check(!store.Save(new(), out var error) && error != null, "Fallo de escritura recuperable");
        Check(!Directory.EnumerateFiles(path, "*.tmp").Any(), "Limpieza tras fallo de escritura");
        Report(previousFailures, "persistencia, corrupción, valores inválidos y escritura bloqueada.");
    }
    private static void Report(int previousFailures, string description) =>
        Console.WriteLine((failures == previousFailures ? "PASS: " : "FAIL: ") + description);

    private static void RunMessageLoop(Func<Task> test)
    {
        using var context = new ApplicationContext();
        using var starter = new System.Windows.Forms.Timer { Interval = 1 };
        starter.Tick += async (_, _) =>
        {
            starter.Stop();
            try { await test(); }
            catch (Exception ex) { failures++; Console.WriteLine("FAIL: " + ex); }
            finally { context.ExitThread(); }
        };
        starter.Start();
        Application.Run(context);
    }
    private static async Task TestPanelAsync()
    {
        int previousFailures = failures;
        var store = new SettingsStore(TestDirectory("panel"));
        using var form = new Form1(store);
        form.Show();
        await Task.Delay(150);
        string previewPath = Path.Combine(Environment.CurrentDirectory, "artifacts", "panel.png");
        Directory.CreateDirectory(Path.GetDirectoryName(previewPath)!);
        using (var preview = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(preview, new Rectangle(Point.Empty, preview.Size));
            preview.Save(previewPath, System.Drawing.Imaging.ImageFormat.Png);
        }
        Console.WriteLine($"Panel renderizado: {previewPath}; DPI={form.DeviceDpi}");
        var host = Field<WallpaperHost>(form, "wallpaper");
        Check(form.Visible && !host.IsActive && host.Status.State == PlaybackState.Inactive, "T01 panel visible sin autoplay");
        var volume = Field<AnimatedSlider>(form, "volumeSlider");
        var intensity = Field<AnimatedSlider>(form, "intensitySlider");
        var apply = Field<AnimatedButton>(form, "applyButton");
        foreach (var valueLabel in new[] { Field<Label>(form, "intensityLabel"), Field<Label>(form, "volumeLabel") })
            Check(valueLabel.Height >= TextRenderer.MeasureText("100 %", valueLabel.Font).Height,
                "Porcentaje: la fila reserva toda la altura del texto");
        await TestButtonPaintingAsync(apply, Field<AnimatedButton>(form, "stopButton"));
        volume.Focus();
        RaiseControlEvent(volume, "OnKeyDown", new KeyEventArgs(Keys.Home));
        Check(volume.Value == 0, "Slider: Home llega a cero");
        RaiseControlEvent(volume, "OnKeyDown", new KeyEventArgs(Keys.Right));
        Check(volume.Value == 1, "Slider: flecha ajusta una unidad");
        RaiseControlEvent(volume, "OnKeyDown", new KeyEventArgs(Keys.PageUp));
        Check(volume.Value == 11, "Slider: PageUp ajusta diez unidades");
        RaiseControlEvent(volume, "OnKeyDown", new KeyEventArgs(Keys.End));
        RaiseControlEvent(volume, "OnKeyDown", new KeyEventArgs(Keys.Up));
        Check(volume.Value == 100, "Slider: límite superior");
        volume.AccessibilityObject.Value = "40";
        Check(volume.Value == 40 && volume.AccessibilityObject.Role == AccessibleRole.Slider, "Slider: valor accesible");
        RaiseControlEvent(volume, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 0, volume.Height / 2, 0));
        RaiseControlEvent(volume, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 0, volume.Height / 2, 0));
        Check(volume.Value == 0 && !volume.Capture, "Slider: puntero y liberación de captura");
        RaiseControlEvent(volume, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, volume.Width / 2, volume.Height / 2, 0));
        RaiseControlEvent(volume, "OnMouseMove", new MouseEventArgs(MouseButtons.Left, 0, volume.Width - 12, volume.Height / 2, 0));
        Check(Math.Abs(volume.VisualPosition - (float)volume.Value / volume.Maximum) < 0.002f,
            "Slider: el indicador sigue el cursor sin retraso durante el arrastre");
        RaiseControlEvent(volume, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, volume.Width - 12, volume.Height / 2, 0));
        volume.Value = 20;
        await Task.Delay(350);
        Check(Math.Abs(volume.VisualPosition - 0.2f) < 0.01f, "Slider: posición visual alcanza el valor");
        if (captureMotion) await RenderMotionAsync(form, apply, intensity, volume);
        Field<TextBox>(form, "urlBox").Text = "javascript:alert(1)";
        Field<Button>(form, "applyButton").PerformClick();
        Check(!host.IsActive && Field<Label>(form, "errorLabel").Text.Length > 0, "T05 error recuperable");
        RaiseControlEvent(apply, "OnMouseEnter", EventArgs.Empty);
        volume.Value = 30;
        form.Close();
        await Task.Delay(40); // Let the next UI timer turn observe ancestor visibility changes.
        Check(!form.Visible && !form.ShowInTaskbar && !form.IsDisposed, "X oculta y mantiene el proceso");
        Check(!apply.IsAnimating && !volume.IsAnimating, "Microanimaciones suspendidas al ocultar el panel");
        form.ShowConfiguration();
        volume.Value = 20;
        Check(form.Visible && form.ShowInTaskbar, "Reapertura de configuración");
        form.GetType().GetMethod("ExitApplication", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(form, null);
        Check(form.IsDisposed && !host.IsActive, "Salida libera panel y host");
        Report(previousFailures, "panel, deslizadores, microanimaciones, ocultación, reapertura y salida.");
    }
    private static void RaiseControlEvent(Control control, string method, EventArgs args) =>
        control.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, new object[] { args });

    private static async Task TestButtonPaintingAsync(AnimatedButton apply, AnimatedButton stop)
    {
        foreach (var button in new[] { apply, stop })
        {
            bool enabled = button.Enabled;
            button.Enabled = true;
            RaiseControlEvent(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0));
            Check(Field<float>(button, "press") > 0, "Microanimación: un clic breve produce feedback antes del primer tick");
            RaiseControlEvent(button, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, -1, -1, 0));
            await Task.Delay(350);
            foreach (string state in new[] { "reposo", "hover", "pulsación", "liberación", "deshabilitado" })
            {
                if (state == "hover") RaiseControlEvent(button, "OnMouseEnter", EventArgs.Empty);
                if (state == "pulsación") RaiseControlEvent(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0));
                if (state == "liberación")
                {
                    RaiseControlEvent(button, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, -1, -1, 0));
                    RaiseControlEvent(button, "OnMouseLeave", EventArgs.Empty);
                }
                if (state == "deshabilitado") button.Enabled = false;
                await Task.Delay(350);
                using var client = new Bitmap(button.Width, button.Height);
                using (var graphics = Graphics.FromImage(client))
                {
                    graphics.Clear(Color.Magenta);
                    nint dc = graphics.GetHdc();
                    try { SendMessage(button.Handle, 0x0318, dc, 0x4); } // WM_PRINTCLIENT / PRF_CLIENT: only our control.
                    finally { graphics.ReleaseHdc(dc); }
                }
                using var parent = new Bitmap(button.Parent!.Width, button.Parent.Height);
                button.Parent.DrawToBitmap(parent, new Rectangle(Point.Empty, parent.Size));
                var points = new[] { Point.Empty, new Point(button.Width - 1, 0),
                    new Point(0, button.Height - 1), new Point(button.Width - 1, button.Height - 1),
                    new Point(button.Width / 2, button.Height - 1) };
                Check(points.All(p => client.GetPixel(p.X, p.Y).ToArgb() == parent.GetPixel(button.Left + p.X, button.Top + p.Y).ToArgb()),
                    $"Botón {(button.Primary ? "Aplicar" : "Detener")}: fondo de esquinas y borde inferior coincide con el panel en {state}");
            }
            button.Enabled = enabled;
            RaiseControlEvent(button, "OnMouseLeave", EventArgs.Empty);
        }
    }

    private static async Task RenderMotionAsync(Form1 form, AnimatedButton apply, AnimatedSlider intensity, AnimatedSlider volume)
    {
        string directory = Path.Combine(Environment.CurrentDirectory, "artifacts", "motion-frames");
        Directory.CreateDirectory(directory);
        Field<TextBox>(form, "urlBox").Text = "https://youtu.be/M7lc1UVf-VE";
        form.ActiveControl = null;
        for (int frame = 0; frame < 60; frame++)
        {
            if (frame == 5) RaiseControlEvent(apply, "OnMouseEnter", EventArgs.Empty);
            if (frame == 15) { volume.Value = 65; RaiseControlEvent(volume, "OnMouseEnter", EventArgs.Empty); }
            if (frame == 30) { intensity.Value = 50; RaiseControlEvent(intensity, "OnMouseEnter", EventArgs.Empty); }
            if (frame == 43) { volume.Value = 20; RaiseControlEvent(volume, "OnMouseLeave", EventArgs.Empty); }
            if (frame == 48) { intensity.Value = 85; RaiseControlEvent(intensity, "OnMouseLeave", EventArgs.Empty); RaiseControlEvent(apply, "OnMouseLeave", EventArgs.Empty); }
            using var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
            // Render our controls, rather than capturing any other windows or desktop content.
            form.Controls[0].DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(directory, $"frame-{frame:D3}.png"), System.Drawing.Imaging.ImageFormat.Png);
            await Task.Delay(32);
        }
        Field<TextBox>(form, "urlBox").Text = "";
        Console.WriteLine("Microanimaciones renderizadas: " + directory);
    }
    private static T Field<T>(object instance, string name) =>
        (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static async Task TestDesktopAsync()
    {
        var layer = DesktopInterop.FindDesktopLayer();
        WallpaperForm form;
        using (DesktopInterop.MatchDpiContext(layer.Parent)) form = new WallpaperForm();
        nint handle = form.SurfaceHandle;
        using (form)
        {
            DesktopInterop.Attach(handle, layer);
            form.Show();
            await Task.Delay(300);
            Check(DesktopInterop.IsAttached(handle, layer), "HWND propio hijo de WorkerW");
            Check(!IsWindowEnabled(handle), "Superficie pasiva deshabilitada");
            long extended = GetWindowLongPtr(handle, -20).ToInt64();
            Check((extended & 0x80) != 0 && (extended & 0x40000) == 0, "Estilos sin ventana de barra/Alt+Tab");
            Console.WriteLine("Capa: " + DesktopInterop.ClassName(layer.Parent) + "; iconos: " + DesktopInterop.ClassName(layer.IconRoot));
        }
        Check(!DesktopInterop.IsWindow(handle), "HWND destruido al detener");
        Console.WriteLine("PASS: acoplamiento y limpieza Win32. Clics y composición visual requieren prueba manual.");
    }
    private static async Task TestPlaybackAsync(string url)
    {
        if (!YouTubeUrlParser.TryParse(url, out var source, out string error)) throw new InvalidOperationException(error);
        var settings = new SettingsStore(TestDirectory("playback"));
        using var form = new Form1(settings);
        form.Show();
        var host = Field<WallpaperHost>(form, "wallpaper");
        host.StatusChanged += (_, status) => Console.WriteLine("Player: " + status.State + " " + status.Message);
        Field<TextBox>(form, "urlBox").Text = url;
        Field<AnimatedSlider>(form, "volumeSlider").Value = 0;
        Field<AnimatedSlider>(form, "intensitySlider").Value = 100;
        Field<Button>(form, "applyButton").PerformClick();
        await WaitForPlayer(host);
        Check(host.Status.State == PlaybackState.Playing, "Evento real playing de YouTube");
        if (host.Status.State == PlaybackState.Playing)
        {
            var firstSurface = Field<WallpaperForm>(host, "surface");
            Check(DesktopInterop.IsWindow(firstSurface.SurfaceHandle), "Host separado del panel");
            form.Close();
            await Task.Delay(1500);
            Check(!form.Visible && host.Status.State == PlaybackState.Playing && host.IsActive, "T10 reproducción con panel oculto");
            using (JsonDocument playing = await Snapshot(firstSurface))
                Check(playing.RootElement.GetProperty("state").GetInt32() == 1, "YouTube confirma playing con panel oculto");
            var tray = Field<TrayController>(form, "tray");
            var icon = Field<NotifyIcon>(tray, "icon");
            Check(icon.Visible, "Bandeja persistente con panel oculto");
            foreach (int value in new[] { 0, 20, 100 })
            {
                Field<AnimatedSlider>(form, "volumeSlider").Value = value;
                await Task.Delay(150);
                using JsonDocument snapshot = await Snapshot(firstSurface);
                Check(snapshot.RootElement.GetProperty("volume").GetInt32() == value &&
                    snapshot.RootElement.GetProperty("muted").GetBoolean() == (value == 0), "T13 volumen API " + value);
            }
            Field<AnimatedSlider>(form, "volumeSlider").Value = 0;
            foreach (int value in new[] { 0, 50, 100 })
            {
                Field<AnimatedSlider>(form, "intensitySlider").Value = value;
                await Task.Delay(150);
                using JsonDocument snapshot = await Snapshot(firstSurface);
                string filter = snapshot.RootElement.GetProperty("filter").GetString()!;
                string expected = "brightness(" + (value / 100.0).ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                Check(filter == expected, "Filtro de intensidad " + value);
            }
            var menu = Field<ContextMenuStrip>(tray, "menu");
            ((ToolStripMenuItem)menu.Items[0]).PerformClick();
            Check(form.Visible && host.Status.State == PlaybackState.Playing, "Reapertura durante reproducción");
            await firstSurface.View.ExecuteScriptAsync("player.pauseVideo()");
            DateTime pauseDeadline = DateTime.UtcNow.AddSeconds(3);
            while (host.Status.State != PlaybackState.Paused && DateTime.UtcNow < pauseDeadline) await Task.Delay(100);
            Check(host.Status.State == PlaybackState.Paused, "Pausa oficial refleja estado en bandeja");
            ((ToolStripMenuItem)menu.Items[1]).PerformClick();
            await WaitForPlayer(host);
            Check(host.Status.State == PlaybackState.Playing, "Reanudar desde bandeja");
            // A second URL replaces all resources; the short URL goes through the same official player.
            YouTubeUrlParser.TryParse("https://youtu.be/aqz-KE-bpKQ", out var replacement, out _);
            Field<TextBox>(form, "urlBox").Text = replacement!.CanonicalUrl.Replace("https://www.youtube.com/watch?v=", "https://youtu.be/");
            ((ToolStripMenuItem)menu.Items[1]).PerformClick();
            await WaitForPlayer(host);
            Check(firstSurface.IsDisposed && Application.OpenForms.OfType<WallpaperForm>().Count() == 1, "T16 sustituir sin duplicar superficies");
            Check(host.Status.State == PlaybackState.Playing && host.Source?.VideoId == replacement!.VideoId, "T03 fuente youtu.be reproducida");
            var lastSurface = Field<WallpaperForm>(host, "surface");
            Field<ToolStripMenuItem>(tray, "stop").PerformClick();
            Check(lastSurface.IsDisposed && !DesktopInterop.IsWindow(lastSurface.SurfaceHandle) &&
                !host.IsActive && icon.Visible, "T15 bandeja detiene y mantiene icono");
            AppSettings recovered = settings.Load(out _);
            Check(recovered.LastUrl == replacement.CanonicalUrl && recovered.Volume == 0 && recovered.Opacity == 100, "Ajustes guardados tras aplicar y detener");
            await Task.Delay(1000);
        }
        host.Stop();
        Check(!host.IsActive && host.Status.State == PlaybackState.Stopped, "Parada del reproductor");
        var exitMenu = Field<ContextMenuStrip>(Field<TrayController>(form, "tray"), "menu");
        ((ToolStripMenuItem)exitMenu.Items[4]).PerformClick();
        Check(form.IsDisposed, "T17 Salir desde el menú de bandeja");
        Console.WriteLine("No se verifican clics físicos, composición visual ni avance de playlist.");
    }
    private static async Task WaitForPlayer(WallpaperHost host)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(48);
        while (host.Status.State is not (PlaybackState.Playing or PlaybackState.Error) && DateTime.UtcNow < deadline)
            await Task.Delay(200);
    }
    private static async Task<JsonDocument> Snapshot(WallpaperForm surface)
    {
        string json = await surface.View.ExecuteScriptAsync("({state: player.getPlayerState(), volume: player.getVolume(), muted: player.isMuted(), filter: getComputedStyle(document.getElementById('player')).filter})");
        return JsonDocument.Parse(json);
    }
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(nint window);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint window, int index);
}
