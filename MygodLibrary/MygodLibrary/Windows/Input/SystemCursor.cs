using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Mygod.Windows.Input
{
    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CursorInfo
        {
            public int cbSize;        // Specifies the size, in bytes, of the structure. 
            // The caller must set this to Marshal.SizeOf(typeof(CURSORINFO)).
            public int flags;         // Specifies the cursor state. This parameter can be one of the following values:
            //    0             The cursor is hidden.
            //    CURSOR_SHOWING    The cursor is showing.
            public IntPtr hCursor;          // Handle to the cursor. 
            public Point ptScreenPos;       // A POINT structure that receives the screen coordinates of the cursor. 
        }

        [DllImport("user32.dll")]
        internal static extern bool GetCursorInfo(out CursorInfo pci);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadCursor(IntPtr hInstance, StandardCursor lpCursorName);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadCursorFromFile(string lpFileName);

        [DllImport("user32.dll")]
        internal static extern bool SetSystemCursor(IntPtr hcur, StandardCursor id);

        [DllImport("user32.dll")]
        internal static extern bool DestroyCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        internal static extern IntPtr CopyIcon(IntPtr hIcon);

        internal struct IconInfo
        {
            public bool Icon;
            public int HotspotX;
            public int HotspotY;
            public IntPtr Mask;
            public IntPtr Color;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

        [DllImport("user32.dll")]
        internal static extern IntPtr CreateIconIndirect(ref IconInfo icon);
    }

    public sealed class SystemCursor : IDisposable
    {
        private SystemCursor(IntPtr handle)
        {
            if ((this.handle = handle) == IntPtr.Zero) Helper.CheckLastWin32Error();
        }
        public SystemCursor(SystemCursor copy) : this(NativeMethods.CopyIcon(copy.handle))
        {
        }
        public SystemCursor(string fileName) : this(NativeMethods.LoadCursorFromFile(fileName))
        {
        }
        public SystemCursor(Bitmap bitmap, Point hotspot)
        {
            var info = new NativeMethods.IconInfo();
            NativeMethods.GetIconInfo(bitmap.GetHicon(), ref info);
            info.HotspotX = hotspot.X;
            info.HotspotY = hotspot.Y;
            info.Icon = false;
            if ((handle = NativeMethods.CreateIconIndirect(ref info)) == IntPtr.Zero) Helper.CheckLastWin32Error();
        }

        public static SystemCursor GetCurrentCursor()
        {
            NativeMethods.CursorInfo info;
            if (!NativeMethods.GetCursorInfo(out info)) throw new Win32Exception(Marshal.GetLastWin32Error());
            return new SystemCursor(info.hCursor);
        }
        public static SystemCursor GetCurrentCursor(StandardCursor cursor)
        {
            return new SystemCursor(NativeMethods.LoadCursor(IntPtr.Zero, cursor));
        }

        private readonly IntPtr handle;

        public void Set(StandardCursor cursor)
        {
            if (!NativeMethods.SetSystemCursor(handle, cursor)) Helper.CheckLastWin32Error();
        }

        public void Dispose()
        {
            NativeMethods.DestroyCursor(handle);
        }
    }

    public enum StandardCursor
    {
        // ReSharper disable InconsistentNaming
        Arrow = 32512, IBeam = 32513, Wait = 32514, Cross = 32515, UpArrow = 32516, [Obsolete] Size = 32640,
        [Obsolete] Icon = 32641, SizeNwSe = 32642, SizeNeSw = 32643, SizeWe = 32644, SizeNs = 32645, SizeAll = 32646,
        No = 32648, Hand = 32649, AppStarting = 32650, Help = 32651
        // ReSharper restore InconsistentNaming
    }

    public sealed class CursorHider
    {
        static CursorHider()
        {
            empty = new Bitmap(32, 32);
            empty.SetPixel(0, 0, Color.FromArgb(1, 0, 0, 0));
        }

        private static readonly Bitmap empty;
        private readonly Dictionary<StandardCursor, SystemCursor> hiddenCursors =
            new Dictionary<StandardCursor, SystemCursor>();

        public bool Show(StandardCursor cursor)
        {
            if (!hiddenCursors.ContainsKey(cursor)) return false;
            var systemCursor = hiddenCursors[cursor];
            systemCursor.Set(cursor);
            hiddenCursors.Remove(cursor);
            systemCursor.Dispose();
            return true;
        }
        public bool Hide(StandardCursor cursor)
        {
            if (hiddenCursors.ContainsKey(cursor)) return false;
            hiddenCursors.Add(cursor, new SystemCursor(SystemCursor.GetCurrentCursor(cursor)));
            new SystemCursor(empty, new Point(0, 0)).Set(cursor);
            return true;
        }
    }
}
