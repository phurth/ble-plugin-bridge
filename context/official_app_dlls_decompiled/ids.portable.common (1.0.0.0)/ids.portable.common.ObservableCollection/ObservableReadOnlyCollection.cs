using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common.ObservableCollection
{
	public class ObservableReadOnlyCollection<TCollection, TItem> : ReadOnlyCollection<TItem>, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable where TCollection : IList<TItem>
	{
		protected internal readonly TCollection BackingCollection;

		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		public event PropertyChangedEventHandler? PropertyChanged;

		public ObservableReadOnlyCollection(TCollection collection)
			: base((IList<TItem>)collection)
		{
			BackingCollection = collection;
			if ((object)collection is INotifyCollectionChanged notifyCollectionChanged)
			{
				notifyCollectionChanged.CollectionChanged += HandleCollectionChanged;
			}
			if ((object)collection is INotifyPropertyChanged notifyPropertyChanged)
			{
				notifyPropertyChanged.PropertyChanged += HandlePropertyChanged;
			}
		}

		private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			OnCollectionChanged(e);
		}

		private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged(e);
		}

		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
		{
			this.CollectionChanged?.Invoke(this, args);
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
		{
			this.PropertyChanged?.Invoke(this, args);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			try
			{
				((INotifyCollectionChanged)base.Items).CollectionChanged -= HandleCollectionChanged;
			}
			catch
			{
			}
			try
			{
				((INotifyPropertyChanged)base.Items).PropertyChanged -= HandlePropertyChanged;
			}
			catch
			{
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		~ObservableReadOnlyCollection()
		{
			Dispose(disposing: false);
		}
	}
}
