using System;

namespace IDS.Portable.Common.ObservableCollection
{
	public interface IItem<out TICategory, out TCategory, out TItem> where TICategory : ICategory<TCategory> where TCategory : IComparable<TCategory>, IEquatable<TCategory> where TItem : IComparable<TItem>, IEquatable<TItem>
	{
		TICategory Category { get; }

		TItem ItemKey { get; }
	}
}
