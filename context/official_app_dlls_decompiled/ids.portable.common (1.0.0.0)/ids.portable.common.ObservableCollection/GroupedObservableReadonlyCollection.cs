using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using ids.portable.common.Collection;
using IDS.Portable.Common.ObservableCollection;

namespace ids.portable.common.ObservableCollection
{
	public class GroupedObservableReadonlyCollection<TCollection, TGroupHeader, TItem> : ObservableReadOnlyCollection<ObservableCollection<object>, object> where TCollection : class, IEnumerable<TItem>, INotifyCollectionChanged where TGroupHeader : class, IGroupHeader, IComparable<TGroupHeader> where TItem : class, IComparable<TItem>
	{
		private readonly IGroupHeaderFactory<TGroupHeader, TItem> _groupHeaderFactory;

		private readonly IItemDividerFactory? _itemDividerFactory;

		private readonly IGroupFooterFactory? _groupFooterFactory;

		private readonly IEnumerable _items;

		private readonly SortedList<TGroupHeader, SortedCollection<TItem>> _groups;

		public GroupedObservableReadonlyCollection(TCollection items, IGroupHeaderFactory<TGroupHeader, TItem> groupHeaderFactory, IItemDividerFactory? itemDividerFactory = null, IGroupFooterFactory? groupFooterFactory = null)
			: base(new ObservableCollection<object>())
		{
			_groupHeaderFactory = groupHeaderFactory;
			_itemDividerFactory = itemDividerFactory;
			_groupFooterFactory = groupFooterFactory;
			_groups = new SortedList<TGroupHeader, SortedCollection<TItem>>();
			_items = items;
			if (_items is INotifyCollectionChanged notifyCollectionChanged)
			{
				notifyCollectionChanged.CollectionChanged += ItemsCollectionChanged;
			}
			AddItems(_items);
		}

		private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
			case NotifyCollectionChangedAction.Add:
			case NotifyCollectionChangedAction.Remove:
			case NotifyCollectionChangedAction.Replace:
				AddItems(e.NewItems);
				RemoveItems(e.OldItems);
				break;
			case NotifyCollectionChangedAction.Reset:
				ClearItems();
				if (e.NewItems != null)
				{
					AddItems(e.NewItems);
				}
				else
				{
					AddItems(_items);
				}
				break;
			}
		}

		private void AddItems(IEnumerable? newItems)
		{
			if (newItems == null)
			{
				return;
			}
			foreach (TItem item in Enumerable.OfType<TItem>(newItems))
			{
				int num = 0;
				TGroupHeader val = _groupHeaderFactory.Get(item);
				int num2 = 0;
				if (!_groups.TryGetValue(val, out var sortedCollection))
				{
					sortedCollection = new SortedCollection<TItem>();
					_groups.Add(val, sortedCollection);
					num = CalculateBackingCollectionIndex(val, _groups, _itemDividerFactory != null, _groupFooterFactory != null);
					BackingCollection.Insert(num, val);
					if (_groupFooterFactory != null)
					{
						BackingCollection.Insert(num + 1, _groupFooterFactory!.Get(val));
					}
				}
				else
				{
					num = CalculateBackingCollectionIndex(val, _groups, _itemDividerFactory != null, _groupFooterFactory != null);
				}
				sortedCollection.Add(item, out var index);
				num2 = CalculateBackingCollectionIndex(index, num, _itemDividerFactory != null);
				BackingCollection.Insert(num2, item);
				if (_itemDividerFactory == null)
				{
					continue;
				}
				IItemDivider itemDivider = _itemDividerFactory!.Get(val);
				int num3 = index;
				if (num3 <= 0)
				{
					if (num3 == 0 && sortedCollection.Count > 1)
					{
						BackingCollection.Insert(num2 + 1, itemDivider);
					}
				}
				else
				{
					BackingCollection.Insert(num2, itemDivider);
				}
			}
		}

		private void RemoveItems(IEnumerable? oldItems)
		{
			if (oldItems == null)
			{
				return;
			}
			foreach (TItem item in Enumerable.OfType<TItem>(oldItems))
			{
				TGroupHeader val = _groupHeaderFactory.Get(item);
				int num = CalculateBackingCollectionIndex(val, _groups, _itemDividerFactory != null, _groupFooterFactory != null);
				SortedCollection<TItem> sortedCollection = _groups[val];
				int num2 = sortedCollection.IndexOf(item);
				int num3 = CalculateBackingCollectionIndex(num2, num, _itemDividerFactory != null);
				sortedCollection.RemoveAt(num2);
				if (_itemDividerFactory != null && sortedCollection.Count != 0)
				{
					BackingCollection.RemoveAt(num3);
				}
				BackingCollection.RemoveAt(num3);
				if (sortedCollection.Count == 0)
				{
					_groups.Remove(val);
					if (_groupFooterFactory != null)
					{
						BackingCollection.RemoveAt(num + 1);
					}
					BackingCollection.RemoveAt(num);
				}
			}
		}

		private void ClearItems()
		{
			_groups.Clear();
			BackingCollection.Clear();
		}

		private static int CalculateBackingCollectionIndex(TGroupHeader groupHeader, SortedList<TGroupHeader, SortedCollection<TItem>> groups, bool addItemDividers, bool addGroupFooters)
		{
			int num = groups.IndexOfKey(groupHeader);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				num2 += groups.Values[i].Count;
				if (addItemDividers)
				{
					num2 += groups.Values[i].Count - 1;
				}
				if (addGroupFooters)
				{
					num2++;
				}
				if (num > 0)
				{
					num2++;
				}
			}
			return num2;
		}

		private static int CalculateBackingCollectionIndex(int itemIndex, int mainGroupIndex, bool addItemDividers)
		{
			if (!addItemDividers || itemIndex == 0)
			{
				return mainGroupIndex + 1 + itemIndex;
			}
			return mainGroupIndex + 2 * itemIndex;
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (!disposing)
			{
				return;
			}
			ClearItems();
			if (!(_items is INotifyCollectionChanged notifyCollectionChanged))
			{
				return;
			}
			try
			{
				notifyCollectionChanged.CollectionChanged -= ItemsCollectionChanged;
			}
			catch
			{
			}
		}
	}
}
