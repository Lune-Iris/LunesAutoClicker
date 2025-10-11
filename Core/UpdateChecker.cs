using System.Text.RegularExpressions;

namespace LAC3.Core;

internal class UpdateChecker
{
    internal static async Task<bool> CheckForUpdates()
    {
        string currentVersionRaw = string.Empty;
        var app = Application.Current;
        if (app?.MainWindow != null)
        {
            app.Dispatcher.Invoke(() =>
            {
                var mw = app.MainWindow;
                var label = mw.FindName("VersionLabel");
                if (label is Label l)
                    currentVersionRaw = l.Content?.ToString() ?? string.Empty;
            });
        }

        var currentVersion = ParseVersionFromLabel(currentVersionRaw);
        if (currentVersion == null)
            return false;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("request");
        var url = "https://api.github.com/repos/Lune-Iris/LunesAutoClicker/releases";
        var json = await http.GetStringAsync(url);

        using var doc = JsonDocument.Parse(json);
        var firstRelease = doc.RootElement[0];
        var tag = firstRelease.GetProperty("tag_name").GetString();

        if (tag?.StartsWith("v", StringComparison.OrdinalIgnoreCase) == true)
            tag = tag[1..];

        return Version.TryParse(tag, out var latest) && latest > currentVersion;
    }

    private static Version? ParseVersionFromLabel(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var m = Regex.Match(raw, @"\d+(\.\d+)+");
        if (m.Success && Version.TryParse(m.Value, out var v))
            return v;

        raw = raw.Trim();
        if (raw.StartsWith("LAC3 ", StringComparison.OrdinalIgnoreCase))
            raw = raw[5..].Trim();
        if (raw.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            raw = raw[1..];

        return Version.TryParse(raw, out var v2) ? v2 : null;
    }
}
