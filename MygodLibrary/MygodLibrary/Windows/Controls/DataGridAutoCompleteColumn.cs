using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace Mygod.Windows.Controls
{
    public class DataGridAutoCompleteColumn : DataGridBoundColumn
    {
        public override BindingBase Binding
        {
            get { return base.Binding; }
            set
            {
                var binding = value as Binding; //如果没指定转换器，指定通用转换器来转
                if (binding != null)
                {
                    if (binding.Converter == null) binding.Converter = new DataGridAutoCompleteColumnConverter();
                    if (binding.Mode == BindingMode.Default) binding.Mode = BindingMode.TwoWay;
                }
                base.Binding = value;
            }
        }

        private IValueConverter Converter
        {
            get
            {
                var binding = Binding as Binding;
                return binding != null ? binding.Converter : null;
            }
            //set { this._converter = value; }
        }

        public AutoCompleteFilterMode FilterMode
        {
            get { return (AutoCompleteFilterMode)GetValue(FilterModeProperty); }
            set { SetValue(FilterModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValueMemberPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FilterModeProperty =
            DependencyProperty.Register("FilterMode", typeof(AutoCompleteFilterMode), typeof(DataGridAutoCompleteColumn),
                                        new PropertyMetadata(AutoCompleteFilterMode.Contains, OnMemberPathPropertyChanged));

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            var block = new TextBlock();
            if (Binding != null && !DesignerProperties.GetIsInDesignMode(block)) block.SetBinding(TextBlock.TextProperty, Binding);
            return block;
        }

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            var box = new AutoCompleteBox
            {
                FilterMode = FilterMode,
                IsTextCompletionEnabled = true,
                BorderThickness = new Thickness(),
                Background = Brushes.White,
                Padding = new Thickness()
            };
            if (Binding != null && !DesignerProperties.GetIsInDesignMode(box))
            {
                box.ItemsSource = ItemsSource;
                var itemTemplate = ItemTemplate;
                if (itemTemplate == null && !string.IsNullOrEmpty(DisplayMemberPath)) itemTemplate = GetItemTemplate(DisplayMemberPath);
                box.ItemTemplate = itemTemplate;
                if (!string.IsNullOrEmpty(DisplayMemberPath)) box.ValueMemberBinding = new Binding(DisplayMemberPath);
                else if (!string.IsNullOrEmpty(ValueMemberPath)) box.ValueMemberPath = ValueMemberPath;
                box.SetBinding(AutoCompleteBox.TextProperty, Binding);
            }
            return box;
        }

        public static DataTemplate GetItemTemplate(string displayMemberPath)
        {
            if (displayMemberPath == null) return null;
            return (DataTemplate)XamlReader.Parse("<DataTemplate xmlns=\"http:/" +
                "/schemas.microsoft.com/winfx/2006/xaml/presentation\"> <TextBlock Text=\"{Binding Path=" + displayMemberPath
                + "}\" /> </DataTemplate>");
        }

        protected override void CancelCellEdit(FrameworkElement editingElement, object uneditedValue)
        {
            //base.CancelCellEdit(editingElement, uneditedValue);
            var box = editingElement as AutoCompleteBox;
            if (box == null) return;
            if (RequiredConverter) box.Text = (string)Converter.Convert(uneditedValue, typeof(string), null, CultureInfo.CurrentCulture);
            else if (uneditedValue != null) box.Text = uneditedValue.ToString();
        }

        protected override object PrepareCellForEdit(FrameworkElement editingElement, RoutedEventArgs editingEventArgs)
        {
            var box = editingElement as AutoCompleteBox;
            if (box == null) return null;
            box.Focus();
            var text = box.Text;
            var args = editingEventArgs as TextCompositionEventArgs;
            if (args != null)
            {
                var str = args.Text;
                box.Text = str;
                box.Select(str.Length, 0);
                return text;
            }
            if (!(editingEventArgs is MouseButtonEventArgs) || !PlaceCaretOnAutoCompleteBox(box, Mouse.GetPosition(box))) box.SelectAll();
            return text;
        }

        private static bool PlaceCaretOnAutoCompleteBox(AutoCompleteBox box, Point position)
        {
            int characterIndexFromPoint = box.GetCharacterIndexFromPoint(position, false);
            if (characterIndexFromPoint >= 0)
            {
                box.Select(characterIndexFromPoint, 0);
                return true;
            }
            return false;
        }

        private bool RequiredConverter
        {
            get
            {
                return !string.IsNullOrEmpty(ValueMemberPath) && !string.IsNullOrEmpty(DisplayMemberPath);
            }
        }

        #region AutoComplete

        public string ValueMemberPath
        {
            get { return GetValue(ValueMemberPathProperty) as string; }
            set { SetValue(ValueMemberPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValueMemberPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueMemberPathProperty =
            DependencyProperty.Register("ValueMemberPath", typeof(string), typeof(DataGridAutoCompleteColumn),
                                        new PropertyMetadata(null, OnMemberPathPropertyChanged));


        public string DisplayMemberPath
        {
            get { return GetValue(DisplayMemberPathProperty) as string; }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DisplayMemberPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(DataGridAutoCompleteColumn),
                                        new PropertyMetadata(null, OnMemberPathPropertyChanged));


        public IEnumerable ItemsSource
        {
            get { return GetValue(ItemsSourceProperty) as IEnumerable; }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(DataGridAutoCompleteColumn),
                                        new PropertyMetadata(null, OnItemsSourcePropertyChanged));


        public DataTemplate ItemTemplate
        {
            get { return GetValue(ItemTemplateProperty) as DataTemplate; }
            set { SetValue(ItemTemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemTemplate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(DataGridAutoCompleteColumn),
                                        new PropertyMetadata(null));

        private static void OnMemberPathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DataGridAutoCompleteColumn;
            if (control != null)
            {
                control.OnMemberPathChanged();
            }
        }


        private void OnMemberPathChanged()
        {
            //set binding converter
            var converter = Converter as DataGridAutoCompleteColumnConverter;
            if (converter == null) return;
            converter.ValueMember = ValueMemberPath;
            converter.DisplayMember = DisplayMemberPath;
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DataGridAutoCompleteColumn;
            if (control != null)
            {
                control.OnItemsSourceChanged();
            }
        }

        private void OnItemsSourceChanged()
        {
            var converter = Converter as DataGridAutoCompleteColumnConverter;
            if (converter != null) converter.ItemsSource = ItemsSource;
        }

        #endregion
    }

    public class DataGridAutoCompleteColumnConverter : IValueConverter
    {
        private IEnumerable itemsSource;
        private PropertyInfo valuePropertyInfo;
        private PropertyInfo displayPropertyInfo;
        private Type elementType;
        private bool initialized;

        public string DisplayMember { get; set; }

        public string ValueMember { get; set; }

        public IEnumerable ItemsSource
        {
            get { return itemsSource; }
            set
            {
                itemsSource = value;
                initialized = false;
            }
        }

        private void Init()
        {
            if (initialized) return;
            if (ItemsSource != null && !string.IsNullOrEmpty(DisplayMember) && !string.IsNullOrEmpty(ValueMember))
            {
                var enumerator = ItemsSource.GetEnumerator();
                enumerator.MoveNext();
                var current = enumerator.Current;
                var type = current.GetType();
                if (!string.IsNullOrEmpty(ValueMember)) valuePropertyInfo = type.GetProperty(ValueMember);
                if (!string.IsNullOrEmpty(DisplayMember)) displayPropertyInfo = type.GetProperty(DisplayMember);
                elementType = type;
                initialized = true;
            }
            else
            {
                valuePropertyInfo = null;
                displayPropertyInfo = null;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(DisplayMember) && string.IsNullOrEmpty(ValueMember)) return value;
            Init();
            if (displayPropertyInfo == null) return value;
            if (targetType == displayPropertyInfo.PropertyType)
            {
                if (ItemsSource == null) return value;
                if (value.GetType() == elementType) return displayPropertyInfo.GetValue(value, null);
                var item = ItemsSource.Cast<object>().FirstOrDefault(o => value.Equals(valuePropertyInfo.GetValue(o, null)));
                if (item != null) return displayPropertyInfo.GetValue(item, null);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Init();
            if (value == null) return null;
            if (displayPropertyInfo != null)
            {
                var item = ItemsSource.Cast<object>().FirstOrDefault(o => value.Equals(displayPropertyInfo.GetValue(o, null)));
                if (item != null && valuePropertyInfo != null) return valuePropertyInfo.GetValue(item, null);
                return item;
            }
            if (valuePropertyInfo != null && value.GetType() == elementType) return valuePropertyInfo.GetValue(value, null);
            if (targetType != null && targetType.IsClass && value as string == string.Empty) return null;
            return value;
        }
    }
}
