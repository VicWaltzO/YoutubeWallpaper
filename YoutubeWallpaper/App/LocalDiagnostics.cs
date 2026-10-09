namespace YoutubeWallpaper.App;

internal sealed class LocalDiagnostics(string directory)
{
    public void Write(string operation, Exception? exception = null)
    {
        try
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "diagnostics.log");
            if (File.Exists(path) && new FileInfo(path).Length > 1024 * 1024) File.Delete(path);
            // Call sites supply fixed operation names. No exception messages, URLs or browser data.
            string detail = exception == null ? "" : $" {exception.GetType().Name} HResult=0x{exception.HResult:X8}";
            File.AppendAllText(path, $"{DateTimeOffset.Now:O} {operation}{detail}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
