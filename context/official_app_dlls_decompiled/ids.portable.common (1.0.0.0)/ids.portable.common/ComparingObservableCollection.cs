using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IDS.Portable.Common
{
	public class ComparingObservableCollection<T> : BaseObservableCollection<T>
	{
		protected readonly bool OrderAscending = true;

		private readonly IComparer<T>? _comparer;

		protected bool UseComparer
		{
			get
			{
				if (_comparer == null && !IntrospectionExtensions.GetTypeInfo(typeof(IComparable<T>)).IsAssignableFrom(IntrospectionExtensions.GetTypeInfo(typeof(T))))
				{
					throw new ArgumentException("IComparer<T> is null and T is not IComparable");
				}
				return _comparer != null;
			}
		}

		public ComparingObservableCollection(Func<T, T, int> comparerFunc, bool orderAscending = true)
			: this((IComparer<T>?)new CustomCompare<T>(comparerFunc), orderAscending)
		{
		}

		public ComparingObservableCollection(bool orderAscending = true)
			: this((IComparer<T>?)null, orderAscending)
		{
		}

		public ComparingObservableCollection(IComparer<T>? comparer, bool orderAscending = true)
		{
			_comparer = comparer;
			OrderAscending = orderAscending;
		}

		public ComparingObservableCollection(IEnumerable<T> items, IComparer<T> comparer, bool orderAscending = true)
		{
			_comparer = comparer;
			OrderAscending = orderAscending;
			using (SuppressEvents(forceRefresh: true))
			{
				foreach (T item in items)
				{
					Add(item);
				}
			}
		}

		protected virtual int CompareItems(T item1, T item2)
		{
			return ((!UseComparer) ? new int?(((IComparable<T>)(object)item1).CompareTo(item2)) : _comparer?.Compare(item1, item2)) ?? throw new ArgumentException("Null argument not supported.");
		}

		protected override void InsertItem(int index, T item)
		{
			base.InsertItem(OrderAscending ? OrderByAscending(item) : OrderByDescending(item), item);
		}

		protected override void SetItem(int index, T item)
		{
			if (CompareItems(base[index], item) == 0)
			{
				base.SetItem(index, item);
				return;
			}
			RemoveAt(index);
			InsertItem(index, item);
		}

		protected override void MoveItem(int oldIndex, int newIndex)
		{
			if (CompareItems(base[oldIndex], base[newIndex]) == 0)
			{
				base.MoveItem(oldIndex, newIndex);
			}
		}

		private int OrderByAscending(T item)
		{
			int num = 0;
			int num2 = base.Count - 1;
			while (num <= num2)
			{
				int num3 = (num + num2) / 2;
				if (CompareItems(item, base[num3]) < 0)
				{
					num2 = num3 - 1;
				}
				else
				{
					num = num3 + 1;
				}
			}
			return num;
		}

		private int OrderByDescending(T item)
		{
			int num = 0;
			int num2 = base.Count - 1;
			while (num <= num2)
			{
				int num3 = (num + num2) / 2;
				if (CompareItems(item, base[num3]) > 0)
				{
					num2 = num3 - 1;
				}
				else
				{
					num = num3 + 1;
				}
			}
			return num;
		}

		public void Sort()
		{
			List<T> list = (UseComparer ? Enumerable.ToList(OrderAscending ? Enumerable.OrderBy(this, (T x) => x, _comparer) : Enumerable.OrderByDescending(this, (T x) => x, _comparer)) : Enumerable.ToList(OrderAscending ? Enumerable.OrderBy(this, (T x) => x) : Enumerable.OrderByDescending(this, (T x) => x)));
			for (int i = 0; i < list.Count; i++)
			{
				int num = IndexOf(list[i]);
				int num2 = i;
				if (num != num2)
				{
					T val = base[num];
					RemoveAt(num);
					base.InsertItem(num2, val);
				}
			}
		}
	}
}
