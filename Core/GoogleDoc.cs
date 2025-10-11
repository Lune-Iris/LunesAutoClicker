namespace LAC3.Core;

internal static class GoogleDoc
{
    private static readonly char[] Separators = ['\r', '\n'];

    internal static async Task GetTitleAsync()
    {
        const string rawUrl = "https://docs.google.com/document/d/1-1X2rkwak3LVsbMYOzIuPgVYVTbBe2UxBaJvrdXIN2U/export?format=txt";

        string[] titles = await FetchTitlesAsync(rawUrl);

        string title = titles.Length > 0
            ? titles[new Random().Next(titles.Length)]
            : "Brain empy :3";

        Application.Current.Dispatcher.Invoke(() =>
        {
            Application.Current.MainWindow.Title = title;
        });
    }

    private static async Task<string[]> FetchTitlesAsync(string fileUrl)
    {
        try
        {
            fileUrl = fileUrl.Trim();

            using HttpClient client = new();
            string content = await client.GetStringAsync(fileUrl);

            string[] lines = content
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToArray();

            return lines.Length > 0
                ? lines
                : ["Brain empy :3"];
        }
        catch (Exception ex)
        {
            SendLogMessage($"Error fetching Google Doc titles: {ex}");
            return ["Brain empy :3"];
        }
    }
}
