namespace YoutubeWallpaper.App;

internal sealed record AppSettings
{
    public int SchemaVersion { get; init; } = 1;
    public string LastUrl { get; init; } = "";
    public int Volume { get; init; } = 20;
    // Retain the requested JSON field; this MVP implements visual intensity, not transparency.
    public int Opacity { get; init; } = 85;
    public string QualityMode { get; init; } = "auto";
    public string VisualMode { get; init; } = "intensity";

    public AppSettings Validate() => new()
    {
        LastUrl = Playback.YouTubeUrlParser.TryParse(LastUrl, out var source, out _) ? source!.CanonicalUrl : "",
        Volume = Volume is >= 0 and <= 100 ? Volume : 20,
        Opacity = Opacity is >= 0 and <= 100 ? Opacity : 85
    };
}
