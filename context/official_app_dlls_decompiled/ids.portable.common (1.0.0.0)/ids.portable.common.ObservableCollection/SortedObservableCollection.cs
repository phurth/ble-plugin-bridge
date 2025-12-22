using System.Collections.Generic;
using System.Collections.ObjectModel;
using IDS.Portable.Common;

namespace ids.portable.common.ObservableCollection
{
	public class SortedObservableCollection<T> : ObservableCollection<T>
	{
		private readonly IComparer<T>? _comparer;

		public SortedObservableCollection()
		{
			_comparer = Comparer<T>.Default;
		}

		public SortedObservableCollection(IComparer<T> comparer)
		{
			_comparer = comparer;
		}

		protected override void InsertItem(int index, T item)
		{
			index = this.BinarySearch(item, _comparer);
			base.InsertItem((index >= 0) ? index : (~index), item);
		}

		protected override void SetItem(int index, T item)
		{
			RemoveAt(index);
			Add(item);
		}

		protected override void MoveItem(int oldIndex, int newIndex)
		{
			T item = base[oldIndex];
			RemoveAt(oldIndex);
			Add(item);
		}
	}
}
