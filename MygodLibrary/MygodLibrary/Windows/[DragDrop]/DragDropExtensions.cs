namespace Mygod.Windows
{
    using System.Windows;

    internal static class DragDropExtensions
    {
        /// <summary>
        /// Converts a System.Windows.Point value to a DragDropLib.Win32Point value.
        /// </summary>
        /// <param name="pt">Input value.</param>
        /// <returns>Converted value.</returns>
        public static Win32Point ToWin32Point(this Point pt)
        {
            return new Win32Point {x = (int) pt.X, y = (int) pt.Y};
        }
    }
}