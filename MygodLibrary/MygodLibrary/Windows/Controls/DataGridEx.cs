using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Mygod.Windows.Controls
{
    public class DataGridEx : DataGrid
    {
        public bool BoundCellLevel
        {
            get { return (bool)GetValue(BoundCellLevelProperty); }
            set { SetValue(BoundCellLevelProperty, value); }
        }

        public static readonly DependencyProperty BoundCellLevelProperty =
            DependencyProperty.Register("BoundCellLevel", typeof(bool), typeof(DataGridEx), new UIPropertyMetadata(true));

        protected override Size MeasureOverride(Size availableSize)
        {
            var desiredSize = base.MeasureOverride(availableSize);
            if (BoundCellLevel) ClearBindingGroup();
            return desiredSize;
        }

        private void ClearBindingGroup()
        {
            // Clear ItemBindingGroup so it isn't applied to new rows
            ItemBindingGroup = null;
            // Clear BindingGroup on already created rows
            foreach (var row in Items.OfType<object>()
                .Select(item => ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement).Where(row => row != null))
                row.BindingGroup = null;
        }
    }
}