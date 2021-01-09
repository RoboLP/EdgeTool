namespace Mygod.Windows
{
    using System.Windows;

    public static class DropTargetHelper
    {
        /// <summary>
        /// Internal instance of the DragDropHelper.
        /// </summary>
        private static readonly IDropTargetHelper SInstance = (IDropTargetHelper) new DragDropHelper();

        static DropTargetHelper()
        {
        }

        /// <summary>
        /// Notifies the DragDropHelper that the specified Window received
        /// a DragEnter event.
        /// </summary>
        /// <param name="window">The Window the received the DragEnter event.</param>
        /// <param name="data">The DataObject containing a drag image.</param>
        /// <param name="cursorOffset">The current cursor's offset relative to the window.</param>
        /// <param name="effect">The accepted drag drop effect.</param>
        public static void DragEnter(Window window, IDataObject data, Point cursorOffset, DragDropEffects effect)
        {
            SInstance.DragEnter(window, data, cursorOffset, effect);
        }

        /// <summary>
        /// Notifies the DragDropHelper that the specified Window received
        /// a DragEnter event.
        /// </summary>
        /// <param name="window">The Window the received the DragEnter event.</param>
        /// <param name="data">The DataObject containing a drag image.</param>
        /// <param name="cursorOffset">The current cursor's offset relative to the window.</param>
        /// <param name="effect">The accepted drag drop effect.</param>
        /// <param name="descriptionMessage">The drop description message.</param>
        /// <param name="descriptionInsert">The drop description insert.</param>
        /// <remarks>Callers of this DragEnter override should make sure to call
        /// the DragLeave override taking an IDataObject parameter in order to clear
        /// the drop description.</remarks>
        public static void DragEnter(Window window, IDataObject data, Point cursorOffset, DragDropEffects effect, string descriptionMessage, string descriptionInsert = null)
        {
            data.SetDropDescription((DropImageType) effect, descriptionMessage, descriptionInsert);
            DragEnter(window, data, cursorOffset, effect);
        }

        /// <summary>
        /// Notifies the DragDropHelper that the current Window received
        /// a DragOver event.
        /// </summary>
        /// <param name="cursorOffset">The current cursor's offset relative to the window.</param>
        /// <param name="effect">The accepted drag drop effect.</param>
        public static void DragOver(Point cursorOffset, DragDropEffects effect)
        {
            SInstance.DragOver(cursorOffset, effect);
        }

        /// <summary>
        /// Notifies the DragDropHelper that the current Window received
        /// a DragLeave event.
        /// </summary>
        public static void DragLeave()
        {
            SInstance.DragLeave();
        }

        /// <summary>
        /// Notifies the DragDropHelper that the current Window received
        /// a DragLeave event.
        /// </summary>
        /// <param name="data">The data object associated to the event.</param>
        public static void DragLeave(IDataObject data)
        {
            data.SetDropDescription(DropImageType.Invalid, null, null);
            DragLeave();
        }

        /// <summary>
        /// Notifies the DragDropHelper that the current Window received
        /// a DragOver event.
        /// </summary>
        /// <param name="data">The DataObject containing a drag image.</param>
        /// <param name="cursorOffset">The current cursor's offset relative to the window.</param>
        /// <param name="effect">The accepted drag drop effect.</param>
        public static void Drop(IDataObject data, Point cursorOffset, DragDropEffects effect)
        {
            SInstance.Drop(data, cursorOffset, effect);
        }

        /// <summary>
        /// Tells the DragDropHelper to show or hide the drag image.
        /// </summary>
        /// <param name="show">True to show the image. False to hide it.</param>
        public static void Show(bool show)
        {
            SInstance.Show(show);
        }
    }
}