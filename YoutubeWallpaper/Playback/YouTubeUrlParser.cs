using System.Text.RegularExpressions;

namespace YoutubeWallpaper.Playback;

internal sealed record YouTubeSource(string? VideoId, string? PlaylistId, int PlaylistIndex, string CanonicalUrl);

internal static partial class YouTubeUrlParser
{
    [GeneratedRegex("^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex VideoPattern();
    [GeneratedRegex("^[A-Za-z0-9_-]{10,150}$", RegexOptions.CultureInvariant)]
    private static partial Regex PlaylistPattern();

    public static bool TryParse(string? input, out YouTubeSource? source, out string error)
    {
        source = null;
        error = "Introduce una URL HTTPS de un vídeo o playlist de YouTube.";
        if (string.IsNullOrWhiteSpace(input) || input.Length > 2048 ||
            !Uri.TryCreate(input.Trim(), UriKind.Absolute, out Uri? uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || uri.UserInfo.Length != 0)
            return false;

        bool shortUrl = uri.Host is "youtu.be" or "www.youtu.be";
        if (!shortUrl && uri.Host is not ("youtube.com" or "www.youtube.com" or "m.youtube.com"))
        {
            error = "Solo se admiten enlaces de youtube.com y youtu.be.";
            return false;
        }

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            foreach (string pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = pair.Split('=', 2);
                string key = Uri.UnescapeDataString(parts[0]);
                if (key is not ("v" or "list" or "index")) continue;
                if (!parameters.TryAdd(key, parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : ""))
                {
                    error = "La URL contiene parámetros de reproducción duplicados.";
                    return false;
                }
            }
        }
        catch (UriFormatException) { return false; }

        string? video = null;
        string? playlist = parameters.GetValueOrDefault("list");
        if (shortUrl)
        {
            string[] segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length != 1) return false;
            video = segments[0];
        }
        else if (uri.AbsolutePath == "/watch") video = parameters.GetValueOrDefault("v");
        else if (uri.AbsolutePath != "/playlist") return false;

        if ((shortUrl || uri.AbsolutePath == "/watch") && (video == null || !VideoPattern().IsMatch(video)))
        {
            error = "El identificador del vídeo no es válido.";
            return false;
        }
        if (playlist != null && !PlaylistPattern().IsMatch(playlist))
        {
            error = "El identificador de la playlist no es válido.";
            return false;
        }
        if (video == null && playlist == null) return false;
        int index = 0;
        if (parameters.TryGetValue("index", out string? indexText))
        {
            if (playlist == null || !int.TryParse(indexText, out int oneBased) || oneBased is < 1 or > 10000)
            {
                error = "La posición de la playlist no es válida.";
                return false;
            }
            index = oneBased - 1;
        }

        string canonical = video != null
            ? "https://www.youtube.com/watch?v=" + video
            : "https://www.youtube.com/playlist?list=" + playlist;
        if (video != null && playlist != null) canonical += "&list=" + playlist;
        if (index > 0) canonical += "&index=" + (index + 1);
        source = new(video, playlist, index, canonical);
        error = "";
        return true;
    }
}
