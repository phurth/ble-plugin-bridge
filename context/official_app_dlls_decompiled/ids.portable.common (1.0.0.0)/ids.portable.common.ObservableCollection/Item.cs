using System;

namespace IDS.Portable.Common.ObservableCollection
{
	public abstract class Item<TICategory, TCategory, TItem> : IItem<TICategory, TCategory, TItem> where TICategory : ICategory<TCategory> where TCategory : IComparable<TCategory>, IEquatable<TCategory> where TItem : IComparable<TItem>, IEquatable<TItem>
	{
		public abstract TICategory Category { get; }

		public abstract TItem ItemKey { get; }
	}
}
