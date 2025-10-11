namespace LAC3.Core;
internal class SendInputs
{
    private static void GetMouseEvent(MouseButton button, bool isDown, out uint dwFlags, out uint mouseData)
    {
        dwFlags = 0; mouseData = 0;
        switch (button)
        {
            case MouseButton.Left: dwFlags = isDown ? 0x0002u : 0x0004u; break;
            case MouseButton.Right: dwFlags = isDown ? 0x0008u : 0x0010u; break;
            case MouseButton.Middle: dwFlags = isDown ? 0x0020u : 0x0040u; break;
            case MouseButton.XButton1: dwFlags = isDown ? 0x0080u : 0x0100u; mouseData = 1; break;
            case MouseButton.XButton2: dwFlags = isDown ? 0x0080u : 0x0100u; mouseData = 2; break;
            default: dwFlags = isDown ? 0x0002u : 0x0004u; break;
        }
    }

    public static void SendMouseInput(ClickerConstruct clicker)
    {
        var bind = clicker.ActionBind;
        GetMouseEvent(bind.Mouse!.Value, true, out uint downFlags, out uint downData);
        var down = new INPUT { type = 0, u = new InputUnion { mi = new MOUSEINPUT { dx = 0, dy = 0, mouseData = downData, dwFlags = downFlags, time = 0, dwExtraInfo = IntPtr.Zero } } };
        clicker.MouseBuffer[0] = down;
        SendInput(1, clicker.MouseBuffer, Marshal.SizeOf<INPUT>());

        if (clicker.HoldDuration != 0)
            Thread.Sleep(clicker.HoldDuration);

        GetMouseEvent(bind.Mouse!.Value, false, out uint upFlags, out uint upData);
        var up = down;
        up.u.mi.dwFlags = upFlags;
        up.u.mi.mouseData = upData;
        clicker.MouseBuffer[0] = up;
        SendInput(1, clicker.MouseBuffer, Marshal.SizeOf<INPUT>());

        if (clicker.Delay != 0)
            Thread.Sleep(clicker.GetRandomDelay());
    }

    public static void SendKeyboardInput(ClickerConstruct clicker)
    {
        ushort scan = clicker._actionScanCode!.Value;
        var down = new INPUT { type = 1, u = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = scan, dwFlags = 0x0008, time = 0, dwExtraInfo = IntPtr.Zero } } };
        clicker.KeyBuffer[0] = down;
        SendInput(1, clicker.KeyBuffer, Marshal.SizeOf<INPUT>());

        if (clicker.HoldDuration != 0)
            Thread.Sleep(clicker.HoldDuration);

        var up = down;
        up.u.ki.dwFlags |= 0x0002;
        clicker.KeyBuffer[0] = up;
        SendInput(1, clicker.KeyBuffer, Marshal.SizeOf<INPUT>());

        if (clicker.Delay != 0)
            Thread.Sleep(clicker.GetRandomDelay());
    }
}