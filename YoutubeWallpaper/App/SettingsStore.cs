using System.Text.Json;

namespace YoutubeWallpaper.App;

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, WriteIndented = true
    };
    public string DirectoryPath { get; }
    public string FilePath => Path.Combine(DirectoryPath, "settings.json");
    public SettingsStore(string? directory = null) => DirectoryPath = directory ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YouTubeWallpaper");

    public AppSettings Load(out string? notice)
    {
        notice = null;
        try
        {
            if (!File.Exists(FilePath)) return new();
            using var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            if (stream.Length > 65536) throw new JsonException();
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(stream, Options);
            if (settings == null) throw new JsonException();
            if (settings.SchemaVersion != 1)
            {
                notice = "La configuración tiene otra versión. Se usarán los valores iniciales.";
                return new();
            }
            return settings.Validate();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            notice = "No se pudo leer la configuración; se usarán los valores iniciales.";
            return new();
        }
    }

    public bool Save(AppSettings settings, out string? error)
    {
        error = null;
        string temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, settings.Validate(), Options);
                stream.Flush(flushToDisk: true);
            }
            for (int attempt = 0; ; attempt++)
            {
                try { File.Move(temporary, FilePath, overwrite: true); break; }
                // A virus scanner or editor may briefly hold the destination during replacement.
                catch (IOException ex) when (attempt < 2 && (ex.HResult & 0xFFFF) is 32 or 33)
                { Thread.Sleep(25); }
                // Windows can report an outstanding delete/replace lock as ACCESS_DENIED.
                // Permanent permission errors still surface after this bounded retry.
                catch (UnauthorizedAccessException) when (attempt < 2)
                { Thread.Sleep(25); }
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            new LocalDiagnostics(DirectoryPath).Write("settings-save-failed", ex);
            error = "No se pudieron guardar los ajustes. Comprueba los permisos de la carpeta de datos.";
            return false;
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}
