using System;
using System.Runtime.InteropServices;

namespace SubTask.PanelNavigation
{
    internal class TouchSimulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool InitializeTouchInjection(uint maxCount, uint dwMode);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool InjectTouchInput(uint count, [In] TOUCHINPUT[] contacts);

        [StructLayout(LayoutKind.Sequential)]
        private struct TOUCHINPUT
        {
            public int x;               // Touch x coordinate (100th of a pixel)
            public int y;               // Touch y coordinate (100th of a pixel)
            public IntPtr hSource;      // Touch source handle (not used)
            public uint dwID;           // Contact ID
            public uint dwFlags;        // Touch event flags
            public uint dwMask;         // Touch data mask
            public uint dwTime;         // TrialEvent for the event
            public IntPtr dwExtraInfo;  // Extra information (not used)
            public uint cxContact;      // Width of the contact area
            public uint cyContact;      // Height of the contact area
        }

        private const uint POINTER_FLAG_NONE = 0x0000;
        private const uint POINTER_FLAG_DOWN = 0x0002;
        private const uint POINTER_FLAG_MOVE = 0x0001;
        private const uint POINTER_FLAG_UP = 0x0004;

        public TouchSimulator()
        {
            // Initialize touch injection with support for 10 contacts
            if (!InitializeTouchInjection(10, 1))
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to initialize touch injection. Error code: {errorCode}");
            }
        }

        public void SendTouch(int x, int y, bool isDown, bool isUp, uint contactId = 0)
        {
            Console.WriteLine($"Sending Touch: ${x}, ${y}");
            TOUCHINPUT touch = new TOUCHINPUT
            {
                x = x,
                y = y,
                dwID = contactId,
                dwFlags = isDown ? POINTER_FLAG_DOWN : (isUp ? POINTER_FLAG_UP : POINTER_FLAG_MOVE),
                dwMask = 0x0002 | 0x0004, // Position and contact area
                cxContact = 10 * 100,    // Example contact size
                cyContact = 10 * 100
            };

            InjectTouchInput(1, new[] { touch });
        }
    }
}
