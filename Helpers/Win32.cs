using System;
using System.Runtime.InteropServices;

namespace Deskout.Helpers
{
    public static class Win32
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, string pwszReason);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        // Win32 constants
        public const uint EWX_LOGOFF = 0x00000000;
        public const uint EWX_SHUTDOWN = 0x00000001;
        public const uint EWX_REBOOT = 0x00000002;
        public const uint EWX_FORCE = 0x00000004;
        public const uint EWX_POWEROFF = 0x00000008;
        public const uint EWX_FORCEIFHUNG = 0x00000010;

        public const uint SHTDN_REASON_MAJOR_OTHER = 0x00000000;
        public const uint SHTDN_REASON_MINOR_OTHER = 0x00000000;
        public const uint SHTDN_REASON_FLAG_PLANNED = 0x80000000;

        public const int WM_QUERYENDSESSION = 0x0011;
        public const int WM_ENDSESSION = 0x0016;

        public const uint ENDSESSION_CLOSEAPP = 0x00000001;
        public const uint ENDSESSION_CRITICAL = 0x40000000;
        public const uint ENDSESSION_LOGOFF = 0x80000000;
    }
}
