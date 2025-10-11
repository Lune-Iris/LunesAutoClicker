using InputInterceptorNS;

namespace LAC3.Core
{
    internal static class InterceptorInputSender
    {
        private static readonly object _initLock = new();
        private static bool _initialized = false;
        private static MouseHook? _mouseHook;
        private static KeyboardHook? _keyboardHook;

        internal static void InstallDriver()
        {
            InputInterceptor.InstallDriver();
            SendLogMessage($"is Interceptor Driver Installed: {InputInterceptor.CheckDriverInstalled()}");
        }

        internal static void UninstallDriver()
        {
            if (InputInterceptor.CheckDriverInstalled())
                InputInterceptor.UninstallDriver();
        }

        private static bool EnsureInitialized()
        {
            lock (_initLock)
            {
                if (_initialized) return true;

                if (!InputInterceptor.CheckDriverInstalled())
                {
                    return false;
                }

                if (!InputInterceptor.Initialize())
                {
                    return false;
                }
                _mouseHook = new MouseHook();
                _keyboardHook = new KeyboardHook();

                _initialized = true;
                return true;
            }
        }

        public static void SendMouse(ClickerConstruct clicker)
        {
            if (!EnsureInitialized()) return;
            if (!clicker.ActionBind.IsMouse || clicker.ActionBind.Mouse == null) return;
            var btn = clicker.ActionBind.Mouse.Value;

            switch (btn)
            {
                case MouseButton.Left:
                    _mouseHook!.SimulateLeftButtonDown();
                    if (clicker.HoldDuration != 0) Thread.Sleep(clicker.HoldDuration);
                    _mouseHook.SimulateLeftButtonUp();
                    break;

                case MouseButton.Right:
                    _mouseHook!.SimulateRightButtonDown();
                    if (clicker.HoldDuration != 0) Thread.Sleep(clicker.HoldDuration);
                    _mouseHook.SimulateRightButtonUp();
                    break;

                case MouseButton.Middle:
                    _mouseHook!.SimulateMiddleButtonDown();
                    if (clicker.HoldDuration != 0) Thread.Sleep(clicker.HoldDuration);
                    _mouseHook.SimulateMiddleButtonUp();
                    break;

                case MouseButton.XButton1:
                case MouseButton.XButton2:
                    _mouseHook!.SimulateRightButtonDown();
                    if (clicker.HoldDuration != 0) Thread.Sleep(clicker.HoldDuration);
                    _mouseHook.SimulateRightButtonUp();
                    break;

                default:
                    _mouseHook!.SimulateLeftButtonDown();
                    if (clicker.HoldDuration != 0) Thread.Sleep(clicker.HoldDuration);
                    _mouseHook.SimulateLeftButtonUp();
                    break;
            }
            if (clicker.Delay != 0)
                Thread.Sleep(clicker.GetRandomDelay());
        }

        public static void SendKeyboard(ClickerConstruct clicker)
        {
            if (!EnsureInitialized()) return;
            if (!clicker.ActionBind.IsKey || clicker.ActionBind.Key == null) return;
            var keyName = clicker.ActionBind.Key.Value.ToString();
            if (!Enum.TryParse(keyName, true, out KeyCode keyCode))
            {
                if (clicker._actionScanCode.HasValue)
                    keyCode = (KeyCode)clicker._actionScanCode.Value;
                else return;
            }
            _keyboardHook!.SimulateKeyDown(keyCode);
            if (clicker.HoldDuration != 0) Thread.Sleep(clicker.HoldDuration);
            _keyboardHook.SimulateKeyUp(keyCode);
            if (clicker.Delay != 0)
                Thread.Sleep(clicker.GetRandomDelay());
        }

        public static void Dispose()
        {
            lock (_initLock)
            {
                try { _keyboardHook?.Dispose(); } catch { }
                try { _mouseHook?.Dispose(); } catch { }
                _keyboardHook = null;
                _mouseHook = null;
                _initialized = false;
            }
        }
    }
}
