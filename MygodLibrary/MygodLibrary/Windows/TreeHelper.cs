using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Mygod.Windows
{
    public static class TreeHelper
    {
        public static T FindVisualParent<T>(this DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T))
                if (source is Visual || source is Visual3D) source = VisualTreeHelper.GetParent(source);
                else source = LogicalTreeHelper.GetParent(source);
            return source as T;
        }

        public static T FindVisualChild<T>(this DependencyObject obj, string name = null) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var test = child as T;
                if (test != null && (name == null || test is FrameworkElement && (test as FrameworkElement).Name == name))
                    return test;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

        public static T FindLogicalParent<T>(this DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T)) source = LogicalTreeHelper.GetParent(source);
            return source as T;
        }

        /// <summary>
        /// Retrieves all the visual children of a framework element.
        /// </summary>
        /// <param name="parent">The parent framework element.</param>
        /// <returns>The visual children of the framework element.</returns>
        public static IEnumerable<DependencyObject> GetVisualChildren(this DependencyObject parent)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var counter = 0; counter < childCount; counter++)
                yield return VisualTreeHelper.GetChild(parent, counter);
        }

        /// <summary>
        /// Retrieves all the logical children of a framework element using a 
        /// breadth-first search.  A visual element is assumed to be a logical 
        /// child of another visual element if they are in the same namescope.
        /// For performance reasons this method manually manages the queue 
        /// instead of using recursion.
        /// </summary>
        /// <param name="parent">The parent framework element.</param>
        /// <returns>The logical children of the framework element.</returns>
        public static IEnumerable<FrameworkElement> GetLogicalChildrenBreadthFirst(this FrameworkElement parent)
        {
            var queue = new Queue<FrameworkElement>(parent.GetVisualChildren().OfType<FrameworkElement>());
            while (queue.Count > 0)
            {
                var element = queue.Dequeue();
                yield return element;
                foreach (var visualChild in element.GetVisualChildren().OfType<FrameworkElement>())
                    queue.Enqueue(visualChild);
            }
        }
    }
}
