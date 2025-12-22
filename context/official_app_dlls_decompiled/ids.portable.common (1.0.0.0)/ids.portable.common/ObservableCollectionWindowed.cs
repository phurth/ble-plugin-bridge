using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace IDS.Portable.Common
{
	public class ObservableCollectionWindowed<TValue, TCollection> : ObservableCollectionWindowed<TValue> where TCollection : ObservableCollection<TValue>, new()
	{
		public ObservableCollectionWindowed(int windowSize, int windowStartIndex = 0)
			: base((ObservableCollection<TValue>)new TCollection(), windowSize, windowStartIndex)
		{
		}
	}
	public class ObservableCollectionWindowed<TValue> : BaseObservableCollection<TValue>
	{
		public readonly ObservableCollection<TValue> BackingCollection;

		private int _windowSize;

		private int _windowStartIndex;

		public int WindowSize
		{
			get
			{
				return _windowSize;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentException("WindowSize can't be less then 0");
				}
				if (_windowSize != value)
				{
					_windowSize = value;
					SyncWithBackingCollection(clearFirst: true);
				}
			}
		}

		private int MaxVisibleCount => Math.Min(WindowSize, Math.Max(BackingCollection.Count - WindowStartIndex, 0));

		public int WindowStartIndex
		{
			get
			{
				return _windowStartIndex;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentException("WindowStartIndex can't be less then 0");
				}
				if (_windowStartIndex != value)
				{
					_windowStartIndex = value;
					SyncWithBackingCollection(clearFirst: true);
				}
			}
		}

		public ObservableCollectionWindowed(ObservableCollection<TValue> backingCollection, int windowSize, int windowStartIndex = 0)
		{
			BackingCollection = backingCollection;
			_windowSize = windowSize;
			_windowStartIndex = windowStartIndex;
			SyncWithBackingCollection(clearFirst: false);
			BackingCollection.CollectionChanged += OnBackingCollectionChanged;
		}

		protected override void InsertItem(int index, TValue item)
		{
			BackingCollection.Insert(index, item);
		}

		protected override void RemoveItem(int index)
		{
			BackingCollection.RemoveAt(index);
		}

		protected override void SetItem(int index, TValue item)
		{
			BackingCollection[index] = item;
		}

		public void SyncWithBackingCollection(bool clearFirst)
		{
			if (clearFirst)
			{
				for (int num = base.Count - 1; num >= 0; num--)
				{
					base.RemoveItem(num);
				}
			}
			int num2 = WindowStartIndex;
			int num3 = 0;
			while (num3 < WindowSize && num2 < BackingCollection.Count)
			{
				TValue val = BackingCollection[num2];
				if (num3 >= base.Count)
				{
					base.InsertItem(base.Count, val);
				}
				else if (!object.Equals(base[num3], val))
				{
					base.InsertItem(num3, val);
				}
				num3++;
				num2++;
			}
			int num4 = base.Count - 1;
			while (num4 >= 0 && (num4 >= MaxVisibleCount || num4 >= WindowSize))
			{
				base.RemoveItem(num4);
				num4--;
			}
		}

		private void OnBackingCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
		{
			switch (eventArgs.Action)
			{
			case NotifyCollectionChangedAction.Reset:
				Clear();
				break;
			case NotifyCollectionChangedAction.Add:
			case NotifyCollectionChangedAction.Remove:
			case NotifyCollectionChangedAction.Replace:
			case NotifyCollectionChangedAction.Move:
				SyncWithBackingCollection(clearFirst: false);
				break;
			}
		}
	}
}
