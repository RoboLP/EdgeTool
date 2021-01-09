using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Microsoft.Win32;

namespace Mygod.Windows
{
    public class CursorWindow : Window
    {
        static CursorWindow()
        {
            TopmostProperty.OverrideMetadata(typeof(CursorWindow), new FrameworkPropertyMetadata(true));
            WindowStyleProperty.OverrideMetadata(typeof(CursorWindow), new FrameworkPropertyMetadata(WindowStyle.None));
            ResizeModeProperty.OverrideMetadata(typeof(CursorWindow), new FrameworkPropertyMetadata(ResizeMode.NoResize));
            AllowsTransparencyProperty.OverrideMetadata(typeof(CursorWindow), new FrameworkPropertyMetadata(true));
            ShowInTaskbarProperty.OverrideMetadata(typeof(CursorWindow), new FrameworkPropertyMetadata(false));
        }

        public CursorWindow()
        {
            Loaded += OnLoad;
        }

        private IntPtr handle;

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern int SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        protected virtual void OnLoad(object sender, RoutedEventArgs e)
        {
            /************************************
             * GWL_EXSTYLE          = -20       *
             * WS_EX_TRANSPARENT    = 0x0000020 *
             * WS_EX_LAYERED        = 0x0080000 *
             * WS_EX_NOACTIVATE     = 0x8000000 * (prevent the window from showing in Alt+Tab)
             * GW_OWNER             = 4         *
             * SWP_NOMOVE           = 0x02      *
             * SWP_NOSIZE           = 0x01      *
             * SWP_NOACTIVATE       = 0x10      *
             ************************************/
            SetWindowLong(handle = new WindowInteropHelper(this).Handle, -20, GetWindowLong(handle, -20) | 0x8080020);
            var owner = GetWindow(handle, 4);
            if (owner != IntPtr.Zero) SetWindowPos(owner, -1, 0, 0, 0, 0, 0x13);    // show in taskbar fixes
            UpdateSize(sender, e);
            SystemEvents.DisplaySettingsChanged += UpdateSize;
        }

        private void UpdateSize(object sender, EventArgs e)
        {
            var rect = default(Rectangle);
            rect = Screen.AllScreens.Select(screen => screen.Bounds)
                .Aggregate(rect, (current, bounds) => current == default(Rectangle) ? bounds : Rectangle.Union(current, bounds));
            Left = rect.Left;
            Top = rect.Top;
            Width = rect.Right - rect.Left;
            Height = rect.Bottom - rect.Top;
        }

        public void BringWindowToTop()
        {
            BringWindowToTop(handle);
        }
    }
}
