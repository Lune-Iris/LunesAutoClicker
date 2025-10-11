namespace LAC3.Utilities;
internal class Utils
{
    internal static readonly Random RandomGenerator = new();

    internal static bool
        LowLevelInput = false,
        Replacing = false,
        NameExists = false,
        TextNull = false,
        Loading = false;

    internal static Dictionary<string, ClickerConstruct> klikers = [];
    internal static Dictionary<string, Thread> klikerThreads = [];

    internal static void SendLogMessage(string message) => Application.Current.Dispatcher.Invoke(() =>
    {
        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            var textBox = mainWindow.LogOutputTx;
            if (textBox == null) return;

            var scrollViewer = GetScrollViewer(textBox);
            bool isAtBottom = scrollViewer != null && scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 1;
            textBox.AppendText($"[{DateTime.Now:HH:mm:ss:fff}] {message}\n");
            if (isAtBottom)
            {
                textBox.ScrollToEnd();
            }
        }
    });

    internal static void UpdateProfiles()
    {
        var clickers = GetAllKlikers().ToArray();
        foreach (var clicker in clickers)
        {
            clicker.UpdateActionBind(clicker.ActionBind);
            clicker.UpdateActivationBind(clicker.ActivationBind);
        }
    }

    private static ScrollViewer? GetScrollViewer(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ScrollViewer viewer)
                return viewer;

            var result = GetScrollViewer(child);
            if (result != null)
                return result;
        }
        return null;
    }
}