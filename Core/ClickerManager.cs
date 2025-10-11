namespace LAC3.Core;

internal static class ClickerManager
{
    internal static void CreateKliker(
        string name,
        ClickBind activationBind,
        ClickBind actionBind,
        ushort holdDuration,
        ushort delay,
        ushort maxDelay,
        ushort burstCount,
        bool holdMode,
        bool toggleMode,
        bool burstMode,
        bool shouldSpam = true)
    {
        if (klikers.ContainsKey(name))
        {
            Replacing = true;
            DeleteKliker(name);
            CreateKliker(name, activationBind, actionBind, holdDuration, delay, maxDelay, burstCount, holdMode, toggleMode, burstMode, shouldSpam);
            return;
        }

        var clicker = new ClickerConstruct(
            name,
            activationBind,
            actionBind,
            holdDuration,
            delay,
            maxDelay,
            burstCount,
            holdMode,
            toggleMode,
            burstMode);

        klikers[name] = clicker;
        var thread = new Thread(clicker.ThreadExecute)
        {
            IsBackground = true
        };
        klikerThreads[name] = thread;
        thread.Start();

        if (Replacing)
        {
            Replacing = false;
        }
        else if (shouldSpam)
        {
            SendLogMessage($"Kliker profile '{name}' created successfully.");
        }
    }

    internal static bool UpdateKliker
    (
        string name,
        ClickBind activationBind,
        ClickBind actionBind,
        ushort holdDuration,
        ushort delay,
        ushort maxDelay,
        ushort burstCount,
        bool holdMode,
        bool toggleMode,
        bool burstMode
    )
    {
        if (!klikers.TryGetValue(name, out var existing))
            return false;



        existing.ShouldStop = true;
        if (klikerThreads.TryGetValue(name, out var oldThread))
        {
            oldThread.Join();
            klikerThreads.Remove(name);
        }
        existing.ShouldStop = false;
        existing.WasButtonPressed = false;
        existing.UpdateActivationBind(activationBind);
        existing.UpdateActionBind(actionBind);
        existing.HoldDuration = holdDuration;
        existing.Delay = (ushort)(maxDelay > 0 && delay == 0 ? 1 : delay);
        existing.MaxDelay = maxDelay;
        existing.BurstCount = burstCount;
        existing.HoldMode = holdMode;
        existing.ToggleMode = toggleMode;
        existing.BurstMode = burstMode;
        existing.RecalculateCaches();
        var thread = new Thread(existing.ThreadExecute) { IsBackground = true };
        klikerThreads[name] = thread;
        thread.Start();
        SendLogMessage($"Kliker profile '{name}' updated successfully.");
        return true;
    }

    internal static void DeleteKliker(string name)
    {
        if (!klikers.TryGetValue(name, out var clicker))
        {
            SendLogMessage($"Kliker profile '{name}' not found.");
            return;
        }

        clicker.ShouldStop = true;
        if (klikerThreads.TryGetValue(name, out var thread))
        {
            thread.Join();
            klikerThreads.Remove(name);
        }
        klikers.Remove(name);
        if (!Replacing && !Loading)
            SendLogMessage($"Kliker profile '{name}' deleted.");
    }

    internal static ClickerConstruct? GetKliker(string name)
=> klikers.TryGetValue(name, out var value) ? value : null;

    internal static IEnumerable<ClickerConstruct> GetAllKlikers() => klikers.Values;
    internal static void ClearAll()
    {
        Loading = true;
        foreach (var clicker in klikers.Values)
            clicker.ShouldStop = true;
        klikers.Clear();
        klikerThreads.Clear();
        Loading = false;
        SendLogMessage("Cleared all profiles.");
    }
}
