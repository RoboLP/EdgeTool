namespace Mygod.Collections.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows.Threading;

    [Serializable]
    public abstract class ObservableKeyedCollection<TKey, TItem> : KeyedCollection<TKey, TItem>, INotifyCollectionChanged,
                                                                   INotifyPropertyChanged
    {
        private readonly SimpleMonitor monitor = new SimpleMonitor();
        private const string CountString = "Count";
        private const string IndexerName = "Item[]";
        protected readonly Dispatcher CurrentDispatcher = Dispatcher.CurrentDispatcher;

        [field: NonSerialized]
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        protected ObservableKeyedCollection()
        {
        }

        protected ObservableKeyedCollection(IEqualityComparer<TKey> comparer) : base(comparer)
        {
        }

        protected ObservableKeyedCollection(IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
            : base(comparer, dictionaryCreationThreshold)
        {
        }

        protected IDisposable BlockReentrancy()
        {
            monitor.Enter();
            return monitor;
        }

        protected void CheckReentrancy()
        {
            if ((monitor.Busy && (CollectionChanged != null)) && (CollectionChanged.GetInvocationList().Length > 1))
                throw new InvalidOperationException("Observable Collection Reentrancy Not Allowed!");
        }

        protected override void ClearItems()
        {
            CheckReentrancy();
            base.ClearItems();
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionReset();
        }

        protected override void InsertItem(int index, TItem item)
        {
            CheckReentrancy();
            base.InsertItem(index, item);
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }

        public void Move(int oldIndex, int newIndex)
        {
            MoveItem(oldIndex, newIndex);
        }

        protected virtual void MoveItem(int oldIndex, int newIndex)
        {
            CheckReentrancy();
            var item = base[oldIndex];
            base.RemoveItem(oldIndex);
            base.InsertItem(newIndex, item);
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex);
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged == null) return;
            using (BlockReentrancy()) CurrentDispatcher.Invoke(() => CollectionChanged(this, e));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
        }

        private void OnCollectionReset()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void RemoveItem(int index)
        {
            CheckReentrancy();
            var item = base[index];
            base.RemoveItem(index);
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        }

        protected override void SetItem(int index, TItem item)
        {
            CheckReentrancy();
            var oldItem = base[index];
            base.SetItem(index, item);
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Replace, oldItem, item, index);
        }

        [Serializable]
        private class SimpleMonitor : IDisposable
        {
            private int busyCount;

            public void Dispose()
            {
                busyCount--;
            }

            public void Enter()
            {
                busyCount++;
            }

            public bool Busy => (busyCount > 0);
        }
    }
}