using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Mygod.Windows.Controls
{
    /// <summary>
    /// 表示一个可显示标题的控件，该标题具有一个用于显示内容的可折叠窗口。折叠/展开时会显示动画。
    /// </summary>
    public class AnimatedExpander : Expander
    {
        private FrameworkElement expandSite;

        /// <summary>
        /// 在派生类中被重写后，每当应用程序代码或内部进程调用 System.Windows.FrameworkElement.ApplyTemplate()，都将调用此方法。
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            expandSite = GetTemplateChild("ExpandSite") as FrameworkElement;
            if (expandSite != null)
            {
                expandSite.LayoutTransform = new ScaleTransform(1, 0);
                expandSite.IsVisibleChanged += ReturnVisible;
            }
            Expanded += ExpanderExpanded;
            Collapsed += ExpanderCollapsed;
            IsExpanded = true;
        }

        private void ReturnVisible(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (expandSite != null) expandSite.Visibility = Visibility.Visible;
        }

        private void ExpanderExpanded(object sender, RoutedEventArgs e)
        {
            StartAnimation(1);
        }

        private void ExpanderCollapsed(object sender, RoutedEventArgs e)
        {
            StartAnimation(0);
        }

        private void StartAnimation(double to)
        {
            expandSite?.LayoutTransform.BeginAnimation(ScaleTransform.ScaleYProperty, 
                new DoubleAnimation {To = to, Duration = new Duration(TimeSpan.FromSeconds(0.25))});
        }
    }
}
