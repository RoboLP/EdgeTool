using System.Windows.Controls;

namespace Mygod.Windows
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Media;
    using System.Windows.Media.Effects;

    /// <summary>
    /// WPF Glass Window
    /// Inherit from this window class to enable glass on a WPF window
    /// </summary>
    public class GlassWindow : Window
    {
        #region properties

        /// <summary>
        /// Get determines if AeroGlass is enabled on the desktop. Set enables/disables AreoGlass on the desktop.
        /// </summary>
        protected static bool AeroGlassCompositionEnabled
        {
            get
            {
                try
                {
                    return DesktopWindowManagerNativeMethods.DwmIsCompositionEnabled();
                }
                catch
                {
                    return false;
                }
            }
        }

        public static readonly DependencyProperty DrawOnTitleBarProperty = DependencyProperty.Register("DrawOnTitleBar",
            typeof(bool), typeof(GlassWindow), new PropertyMetadata(false));

        public bool DrawOnTitleBar
        {
            get { return (bool)GetValue(DrawOnTitleBarProperty); }
            set { SetValue(DrawOnTitleBarProperty, value); }
        }

        #endregion

        #region events

        /// <summary>
        /// Fires when the availability of Glass effect changes.
        /// </summary>
        public event EventHandler<AeroGlassCompositionChangedEventArgs> AeroGlassCompositionChanged;

        #endregion

        #region operations

        /// <summary>
        /// Makes the background of current window transparent from both Wpf and Windows Perspective
        /// </summary>
        protected void SetAeroGlassTransparency()
        {
            // Set the Background to transparent from Win32 perpective 
            var source = HwndSource.FromHwnd(WindowHandle);
            if (source != null && source.CompositionTarget != null) source.CompositionTarget.BackgroundColor = Colors.Transparent;

            // Set the Background to transparent from WPF perpective 
            Background = Brushes.Transparent;
        }

        /// <summary>
        /// Resets the AeroGlass exclusion area.
        /// </summary>
        protected void ResetAeroGlass()
        {
            var margins = new Margins(true);
            try
            {
                DesktopWindowManagerNativeMethods.DwmExtendFrameIntoClientArea(WindowHandle, ref margins);
            }
            catch (DllNotFoundException) { }
        }

        #endregion

        #region implementation
        protected IntPtr WindowHandle;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case DwmMessages.WmDwmCompositionChanged:
                case DwmMessages.WmDwmNcRenderingChanged:
                    AeroGlassCompositionChanged.Invoke(this, new AeroGlassCompositionChangedEventArgs(AeroGlassCompositionEnabled));
                    handled = true;
                    break;
                case DwmMessages.WmNcCalcSize:
                    handled = DrawOnTitleBar;
                    break;
                case DwmMessages.WmNcHitTest:
                    if (!(handled = DrawOnTitleBar)) break;
                    var result = IntPtr.Zero;
                    handled = DesktopWindowManagerNativeMethods.DwmDefWindowProc(hwnd, msg, wParam, lParam, ref result) != 0;
                    if (handled) return result;
                    if (WindowState == WindowState.Maximized) return new IntPtr(1); // no necessary to change the size when maximized
                    var mouse = new Point(lParam.ToInt32() & 0xFFFF, lParam.ToInt32() >> 16);
                    const int border = 6;
                    double right = Left + ActualWidth, bottom = Top + ActualHeight;
                    return new IntPtr(
                        mouse.X - Left <= border
                                ? mouse.Y - Top <= border ? 13 : bottom - mouse.Y <= border ? 16 : 10   // top bottom left
                            : right - mouse.X <= border
                                ? mouse.Y - Top <= border ? 14 : bottom - mouse.Y <= border ? 17 : 11   // top bottom right
                                : mouse.Y - Top <= border ? 12 : bottom - mouse.Y <= border ? 15 : 1);  // top bottom normal
                default:
                    handled = false;
                    break;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// OnSourceInitialized
        /// Override SourceInitialized to initialize windowHandle for this window.
        /// A valid windowHandle is available only after the sourceInitialized is completed
        /// </summary>
        /// <param name="e">EventArgs</param>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var interopHelper = new WindowInteropHelper(this);
            WindowHandle = interopHelper.Handle;

            // add Window Proc hook to capture DWM messages
            var source = HwndSource.FromHwnd(WindowHandle);
            if (source != null) source.AddHook(WndProc);

            //ResetAeroGlass();
            SizeChanged += UpdateAero;
            AeroGlassCompositionChanged += UpdateAero;
            OnAeroEnabledChanged(AeroGlassCompositionEnabled);
        }

        #endregion

        #region <Mygod> Aero event

        private void UpdateAero(object sender = null, SizeChangedEventArgs e = null)
        {
            OnAeroEnabledChanged(AeroGlassCompositionEnabled);
        }

        private void UpdateAero(object sender, AeroGlassCompositionChangedEventArgs e)
        {
            OnAeroEnabledChanged(e.GlassAvailable);
        }

        protected void OnAeroEnabledChanged(bool aeroGlassCompositionEnabled)
        {
            try
            {
                if (aeroGlassCompositionEnabled)
                {
                    ResetAeroGlass();
                    SetAeroGlassTransparency();
                    InvalidateVisual();
                    if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor <= 1)   // win7
                        Resources["GlowingEffect"] = new DropShadowEffect
                        {
                            Color = Colors.White, ShadowDepth = 0, RenderingBias = RenderingBias.Quality, BlurRadius = 8
                        };
                }
                else
                {
                    Background = Brushes.White;
                    Resources["GlowingEffect"] = null;
                }
            }
            catch (InvalidOperationException)   // window is too small
            {
            }
        }

        #endregion
    }

    /// <summary>
    /// Event argument for The GlassAvailabilityChanged event
    /// </summary>
    public class AeroGlassCompositionChangedEventArgs : EventArgs
    {
        internal AeroGlassCompositionChangedEventArgs(bool avialbility)
        {
            GlassAvailable = avialbility;
        }

        /// <summary>
        /// The new GlassAvailable state
        /// </summary>
        public bool GlassAvailable { get; }
    }

    internal static class DwmMessages
    {
        internal const int WmDwmCompositionChanged = 0x031E;
        internal const int WmDwmNcRenderingChanged = 0x031F;
        internal const int WmNcCalcSize = 0x0083;
        internal const int WmNcHitTest = 0x0084;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Margins
    {
        private readonly int LeftWidth;      // width of left border that retains its size
        private readonly int RightWidth;     // width of right border that retains its size
        private readonly int TopHeight;      // height of top border that retains its size
        private readonly int BottomHeight;   // height of bottom border that retains its size

        public Margins(bool fullWindow)
        {
            LeftWidth = RightWidth = TopHeight = BottomHeight = (fullWindow ? -1 : 0);
        }
    };

    /// <summary>
    /// Internal class that contains interop declarations for 
    /// functions that are not benign and are performance critical. 
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    internal static class DesktopWindowManagerNativeMethods
    {
        [DllImport("DwmApi.dll")]
        internal static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins m);

        [DllImport("DwmApi.dll", PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DwmIsCompositionEnabled();

        [DllImport("dwmapi.dll")]
        internal static extern int DwmDefWindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref IntPtr plResult);
    }
}
