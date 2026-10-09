namespace YoutubeWallpaper.App;

internal static class AppIcon
{
    public static Icon Load()
    {
        using Stream stream = typeof(AppIcon).Assembly.GetManifestResourceStream("YoutubeWallpaper.Resources.app.ico")
            ?? throw new InvalidOperationException("Falta el icono incluido en la aplicación.");
        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }
}
