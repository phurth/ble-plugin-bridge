using System;
using System.Collections.Generic;

namespace IDS.Portable.Common
{
	public class OrderedObservableCollection<T> : ComparingObservableCollection<T> where T : IComparable<T>
	{
		public OrderedObservableCollection()
			: base(orderAscending: true)
		{
		}

		public OrderedObservableCollection(bool orderAscending)
			: base(orderAscending)
		{
		}

		public OrderedObservableCollection(IEnumerable<T> items, bool orderAscending = true)
			: base(orderAscending)
		{
			using (SuppressEvents(forceRefresh: true))
			{
				foreach (T item in items)
				{
					Add(item);
				}
			}
		}

		protected override int CompareItems(T item1, T item2)
		{
			return item1.CompareTo(item2);
		}
	}
}
