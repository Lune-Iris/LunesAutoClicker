namespace LAC3.Core;

internal struct ClickBind
{
    public Key? Key;
    public MouseButton? Mouse;

    public bool IsKey => Key.HasValue;
    public bool IsMouse => Mouse.HasValue;

    public int ToVKey() => IsKey
        ? KeyInterop.VirtualKeyFromKey(Key!.Value)
        : MouseToVKey(Mouse!.Value);

    private static int MouseToVKey(MouseButton button) => button switch
    {
        MouseButton.Left => 0x01,
        MouseButton.Right => 0x02,
        MouseButton.Middle => 0x04,
        MouseButton.XButton1 => 0x05,
        MouseButton.XButton2 => 0x06,
        _ => 0
    };
}

internal class ClickerConstruct
{
    internal int _activationVKey;
    internal ushort? _actionScanCode;
    public void RecalculateCaches()
    {
        _activationVKey = ActivationBind.ToVKey();
        if (ActionBind.IsKey)
        {
            int vk = KeyInterop.VirtualKeyFromKey(ActionBind.Key!.Value);
            _actionScanCode = (ushort)MapVirtualKey((uint)vk, MAPVK_VK_TO_VSC);
        }
        else
        {
            _actionScanCode = null;
        }
    }
    internal INPUT[] MouseBuffer = new INPUT[1];
    internal INPUT[] KeyBuffer = new INPUT[1];

    internal Action _sendInputAction;
    internal string ClickerName { get; set; }
    public ClickBind ActivationBind { get; set; }
    public ClickBind ActionBind { get; set; }
    internal int HoldDuration { get; set; }
    internal int Delay { get; set; }
    internal int MaxDelay { get; set; }
    internal ushort BurstCount { get; set; }
    internal bool HoldMode { get; set; }
    internal bool ToggleMode { get; set; }
    internal bool BurstMode { get; set; }

    internal bool IsActive, WasButtonPressed, ShouldStop;
    internal int ThreadId;
    internal bool IsMaxDelayZero => MaxDelay == 0;
    internal int GetRandomDelay() => IsMaxDelayZero ? Delay : RandomGenerator.Next(Delay, MaxDelay);

    public ClickerConstruct(
        string clickerName,
        ClickBind activationBind,
        ClickBind actionBind,
        ushort holdDuration,
        ushort delay,
        ushort maxDelay,
        ushort burstCount,
        bool holdMode,
        bool toggleMode,
        bool burstMode)
    {
        ClickerName = clickerName;
        ActivationBind = activationBind;
        ActionBind = actionBind;
        HoldDuration = holdDuration;
        Delay = (maxDelay > 0 && delay == 0) ? 1 : delay;
        MaxDelay = maxDelay;
        BurstCount = burstCount;
        HoldMode = holdMode;
        ToggleMode = toggleMode;
        BurstMode = burstMode;

        _activationVKey = ActivationBind.ToVKey();

        if (ActionBind.IsKey && ActionBind.Key.HasValue)
        {
            int vKey = KeyInterop.VirtualKeyFromKey(ActionBind.Key.Value);
            _actionScanCode = (ushort)MapVirtualKey((uint)vKey, MAPVK_VK_TO_VSC);
        }
        else
        {
            _actionScanCode = null;
        }

        _sendInputAction = ActionBind.IsKey ^ ActionBind.IsMouse
            ? ActionBind.IsKey
                ? () => SendInputs.SendKeyboardInput(this)
                : () => SendInputs.SendMouseInput(this)
            : throw new ArgumentException("blah blah blah blah *crashes cutely*"); // sillier crash message

        if (!(ActivationBind.IsKey ^ ActivationBind.IsMouse))
            throw new ArgumentException("blah blah blah blah *crashes cutely*");
    }

    internal void ThreadExecute()
    {
        ThreadId = Thread.GetCurrentProcessorId();

        while (!ShouldStop)
        {
            bool isDown = (GetAsyncKeyState(_activationVKey) & 0x8000) != 0;
            bool doClicking =
                (ToggleMode && IsActive)
                || (HoldMode && isDown)
                || (isDown && !WasButtonPressed && (ToggleMode || BurstMode));

            if (doClicking)
                HandleClicking(isDown);
            else
                Thread.Sleep(50);

            WasButtonPressed = isDown;
        }
    }

    private void HandleClicking(bool isDown)
    {
        if (isDown && !WasButtonPressed)
        {
            if (ToggleMode) IsActive = !IsActive;
            if (BurstMode) PerformBurstClick();
        }

        if (IsActive || (HoldMode && isDown))
            _sendInputAction();
    }

    private void PerformBurstClick()
    {
        for (int i = 0; i < BurstCount; i++)
            _sendInputAction();
    }

    public void UpdateActionBind(ClickBind newBind)
    {
        ActionBind = newBind;
        RecalculateCaches();
        Action newSendAction;

        if (!LowLevelInput)
        {
            newSendAction = ActionBind.IsKey
                ? () => SendInputs.SendKeyboardInput(this)
                : () => SendInputs.SendMouseInput(this);
        }
        else
        {
            newSendAction = ActionBind.IsKey
                ? () => InterceptorInputSender.SendKeyboard(this)
                : () => InterceptorInputSender.SendMouse(this);
        }

        _sendInputAction = newSendAction;
    }


    public void UpdateActivationBind(ClickBind newBind)
    {
        ActivationBind = newBind;
        RecalculateCaches();
    }
}