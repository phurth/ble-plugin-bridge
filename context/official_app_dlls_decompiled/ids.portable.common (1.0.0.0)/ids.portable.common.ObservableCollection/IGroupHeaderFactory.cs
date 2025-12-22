using System;

namespace ids.portable.common.ObservableCollection
{
	public interface IGroupHeaderFactory<TGroupHeader, TItem> where TGroupHeader : class, IGroupHeader, IComparable<TGroupHeader> where TItem : class, IComparable<TItem>
	{
		TGroupHeader Get(TItem item);
	}
}
