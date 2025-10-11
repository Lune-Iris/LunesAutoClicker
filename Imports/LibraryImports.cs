namespace LAC3.Imports;

internal static partial class LibraryImports
{
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial short GetAsyncKeyState(int vKey);

    [DllImport("winmm.dll")]
    internal static extern uint timeBeginPeriod(uint ms);


    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint MapVirtualKey(uint uCode, uint uMapType);


    internal const uint MAPVK_VK_TO_VSC = 0;

    internal const int RID_INPUT = 0x10000003;
    internal const int RIM_TYPEKEYBOARD = 1;
    internal const int RIM_TYPEMOUSE = 0;
    internal const int WM_INPUT = 0x00FF;
    internal const int WM_MOUSEHWHEEL = 0x020E;

    [DllImport("user32.dll")]
    internal static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll")]
    internal static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll")]
    internal static extern int ToUnicodeEx(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);

    [DllImport("user32.dll")]
    internal static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetKeyboardLayout(uint idThread);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct RAWKEYBOARD
    {
        [FieldOffset(0)] public ushort MakeCode;
        [FieldOffset(2)] public ushort Flags;
        [FieldOffset(4)] public ushort Reserved;
        [FieldOffset(6)] public ushort VKey;
        [FieldOffset(8)] public uint Message;
        [FieldOffset(12)] public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct RAWMOUSE
    {
        [FieldOffset(0)] public ushort usFlags;
        [FieldOffset(4)] public int lLastX;
        [FieldOffset(8)] public int lLastY;
        [FieldOffset(12)] public uint ulButtons;
        [FieldOffset(16)] public uint ulRawButtons;
        [FieldOffset(20)] public int lLastWheel;
        [FieldOffset(24)] public int ulHorizontalWheel;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct RAWINPUT
    {
        [FieldOffset(0)] public RAWINPUTHEADER header;
        [FieldOffset(16)] public RAWMOUSE mouse;
        [FieldOffset(16)] public RAWKEYBOARD keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}
