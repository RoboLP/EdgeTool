namespace Mygod.Collections.ObjectModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;

    [Serializable]
    public abstract class AdvancedObservableKeyedCollection<TKey, TItem> : ObservableKeyedCollection<TKey, TItem>
        where TItem : INotifyPropertyChanged
    {
        protected AdvancedObservableKeyedCollection()
        {
            CollectionChanged += AddListener;
        }

        protected AdvancedObservableKeyedCollection(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
            CollectionChanged += AddListener;
        }

        protected AdvancedObservableKeyedCollection(IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
            : base(comparer, dictionaryCreationThreshold)
        {
            CollectionChanged += AddListener;
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler ItemPropertyChanged;

        private void AddListener(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (var i in e.OldItems) ((INotifyPropertyChanged)i).PropertyChanged -= OnItemPropertyChanged;
            if (e.NewItems != null)
                foreach (var i in e.NewItems) ((INotifyPropertyChanged)i).PropertyChanged += OnItemPropertyChanged;
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ItemPropertyChanged?.Invoke(sender, e);
        }
    }
}
