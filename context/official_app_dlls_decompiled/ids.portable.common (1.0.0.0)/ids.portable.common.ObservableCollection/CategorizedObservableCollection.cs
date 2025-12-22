using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace IDS.Portable.Common.ObservableCollection
{
	public class CategorizedObservableCollection<TCollection, TICategory, TCategory, TIItem, TItem> : CommonDisposable, IList<TCollection>, ICollection<TCollection>, IEnumerable<TCollection>, IEnumerable, INotifyCollectionChanged where TICategory : TCollection, ICategory<TCategory> where TCategory : IComparable<TCategory>, IEquatable<TCategory> where TIItem : TCollection, IItem<TICategory, TCategory, TItem> where TItem : IComparable<TItem>, IEquatable<TItem>
	{
		private class CategorizableComparer<TICategory, TCategory, TIItem, TItem> : IComparer<TCollection> where TICategory : TCollection, ICategory<TCategory> where TCategory : IComparable<TCategory>, IEquatable<TCategory> where TIItem : TCollection, IItem<TICategory, TCategory, TItem> where TItem : IComparable<TItem>, IEquatable<TItem>
		{
			private readonly bool _ascendingCategories;

			private readonly bool _ascendingItems;

			public CategorizableComparer(bool ascendingCategories = true, bool ascendingItems = true)
			{
				_ascendingCategories = ascendingCategories;
				_ascendingItems = ascendingItems;
			}

			public int Compare(TCollection x, TCollection y)
			{
				if (x is TIItem && y is TIItem)
				{
					TIItem val = (TIItem)(object)x;
					TIItem val2 = (TIItem)(object)y;
					int num = (_ascendingCategories ? val.Category.CategoryKey.CompareTo(val2.Category.CategoryKey) : val2.Category.CategoryKey.CompareTo(val.Category.CategoryKey));
					if (num == 0)
					{
						if (!_ascendingItems)
						{
							return val2.ItemKey.CompareTo(val.ItemKey);
						}
						return val.ItemKey.CompareTo(val2.ItemKey);
					}
					return num;
				}
				if (x is TICategory && y is TIItem)
				{
					TICategory val3 = (TICategory)(object)x;
					TIItem val4 = (TIItem)(object)y;
					int num2 = (_ascendingCategories ? val3.CategoryKey.CompareTo(val4.Category.CategoryKey) : val4.Category.CategoryKey.CompareTo(val3.CategoryKey));
					if (num2 == 0)
					{
						if (!_ascendingCategories)
						{
							return 1;
						}
						return -1;
					}
					return num2;
				}
				if (x is TIItem && y is TICategory)
				{
					TIItem val5 = (TIItem)(object)x;
					TICategory val6 = (TICategory)(object)y;
					int num3 = (_ascendingCategories ? val5.Category.CategoryKey.CompareTo(val6.CategoryKey) : val6.CategoryKey.CompareTo(val5.Category.CategoryKey));
					if (num3 == 0)
					{
						if (!_ascendingCategories)
						{
							return -1;
						}
						return 1;
					}
					return num3;
				}
				if (x is TICategory && y is TICategory)
				{
					TICategory val7 = (TICategory)(object)x;
					TICategory val8 = (TICategory)(object)y;
					if (!_ascendingCategories)
					{
						return val8.CategoryKey.CompareTo(val7.CategoryKey);
					}
					return val7.CategoryKey.CompareTo(val8.CategoryKey);
				}
				throw new ArgumentException("");
			}
		}

		private readonly bool _autoRemoveCategory;

		private readonly ComparingObservableCollection<TCollection> _collection;

		private readonly Dictionary<TCategory, int> _categories;

		TCollection IList<TCollection>.this[int index]
		{
			get
			{
				return _collection[index];
			}
			set
			{
				_collection[index] = value;
			}
		}

		public int Count => _collection.Count;

		public bool IsReadOnly => false;

		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		public CategorizedObservableCollection(bool ascendingCategories = true, bool ascendingItems = true, bool autoRemoveCategory = true)
		{
			_autoRemoveCategory = autoRemoveCategory;
			CategorizableComparer<TICategory, TCategory, TIItem, TItem> comparer = new CategorizableComparer<TICategory, TCategory, TIItem, TItem>(ascendingCategories, ascendingItems);
			_collection = new ComparingObservableCollection<TCollection>(comparer);
			_collection.CollectionChanged += OnCollectionChanged;
			_categories = new Dictionary<TCategory, int>();
		}

		public void Sort()
		{
			Sort(new TCategory[0]);
		}

		public void Sort(TICategory cleanCategory)
		{
			Sort(new TCategory[1] { cleanCategory.CategoryKey });
		}

		public void Sort(bool cleanCategories)
		{
			Sort(cleanCategories ? Enumerable.ToArray(_categories.Keys) : new TCategory[0]);
		}

		protected virtual void Sort(TCategory[] cleanCategories)
		{
			_collection.Sort();
			int num = 0;
			while (num < _collection.Count)
			{
				TCollection val = _collection[num];
				if (val is TICategory)
				{
					TICategory val2 = (TICategory)(object)val;
					if (Enumerable.Contains(cleanCategories, val2.CategoryKey))
					{
						if (num == _collection.Count - 1)
						{
							_categories.Remove(val2.CategoryKey);
							_collection.RemoveAt(num);
						}
						else if (_collection[num + 1] is TICategory)
						{
							_categories.Remove(val2.CategoryKey);
							_collection.RemoveAt(num);
							continue;
						}
					}
				}
				num++;
			}
		}

		public override void Dispose(bool disposing)
		{
			_collection.CollectionChanged -= OnCollectionChanged;
			Clear();
		}

		public int IndexOf(TCollection item)
		{
			if (item is TIItem)
			{
				TIItem item2 = (TIItem)(object)item;
				return IndexOf(item2);
			}
			if (item is TICategory)
			{
				TICategory item3 = (TICategory)(object)item;
				return IndexOf(item3);
			}
			return -1;
		}

		public void Insert(int index, TCollection item)
		{
			Add(item);
		}

		public void RemoveAt(int index)
		{
			Remove(_collection[index]);
		}

		protected int IndexOf(TIItem item)
		{
			for (int i = 0; i < _collection.Count; i++)
			{
				TCollection val = _collection[i];
				if (val is TIItem)
				{
					TIItem val2 = (TIItem)(object)val;
					if (item.Equals(val2))
					{
						return i;
					}
				}
			}
			return -1;
		}

		protected int IndexOf(TICategory item)
		{
			for (int i = 0; i < _collection.Count; i++)
			{
				TCollection val = _collection[i];
				if (val is TICategory)
				{
					TICategory val2 = (TICategory)(object)val;
					if (item.Equals(val2))
					{
						return i;
					}
				}
			}
			return -1;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<TCollection> GetEnumerator()
		{
			return _collection.GetEnumerator();
		}

		public void Add(TCollection item)
		{
			if (item is TIItem)
			{
				TIItem item2 = (TIItem)(object)item;
				AddItem(item2);
			}
			if (item is TICategory)
			{
				TICategory category = (TICategory)(object)item;
				AddCategory(category);
			}
		}

		public void Clear()
		{
			_collection.Clear();
			_categories.Clear();
		}

		public bool Contains(TCollection item)
		{
			return _collection.Contains(item);
		}

		public void CopyTo(TCollection[] array, int arrayIndex)
		{
			_collection.CopyTo(array, arrayIndex);
		}

		public bool Remove(TCollection item)
		{
			if (item is TIItem)
			{
				TIItem item2 = (TIItem)(object)item;
				return RemoveItem(item2);
			}
			if (item is TICategory)
			{
				TICategory category = (TICategory)(object)item;
				return RemoveCategory(category);
			}
			return false;
		}

		protected virtual void AddItem(TIItem item)
		{
			AddCategory(item.Category);
			_categories[item.Category.CategoryKey]++;
			_collection.Add((TCollection)(object)item);
		}

		protected virtual void AddCategory(TICategory category)
		{
			if (!_categories.ContainsKey(category.CategoryKey))
			{
				_categories.Add(category.CategoryKey, 0);
				_collection.Add((TCollection)(object)category);
			}
		}

		protected virtual bool RemoveItem(TIItem item)
		{
			if (!_collection.Contains((TCollection)(object)item))
			{
				return false;
			}
			_collection.Remove((TCollection)(object)item);
			if (_categories.ContainsKey(item.Category.CategoryKey))
			{
				_categories[item.Category.CategoryKey]--;
			}
			if (_autoRemoveCategory && _categories[item.Category.CategoryKey] < 1)
			{
				RemoveCategory(item.Category);
			}
			return true;
		}

		protected virtual bool RemoveCategory(TICategory category)
		{
			if (!_categories.ContainsKey(category.CategoryKey))
			{
				return false;
			}
			if (_categories[category.CategoryKey] > 0)
			{
				int num = 0;
				while (num < _collection.Count)
				{
					TCollection val = _collection[num];
					if (val is TIItem && ((TIItem)(object)val).Category.CategoryKey.CompareTo(category.CategoryKey) == 0)
					{
						_collection.RemoveAt(num);
					}
					else
					{
						num++;
					}
				}
			}
			_categories.Remove(category.CategoryKey);
			_collection.Remove((TCollection)(object)category);
			return true;
		}

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
		{
			this.CollectionChanged?.Invoke(sender, notifyCollectionChangedEventArgs);
		}
	}
}
