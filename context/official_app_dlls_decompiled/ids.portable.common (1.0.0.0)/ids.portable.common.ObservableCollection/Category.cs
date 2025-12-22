using System;

namespace IDS.Portable.Common.ObservableCollection
{
	public abstract class Category<TCategory> : ICategory<TCategory> where TCategory : IComparable<TCategory>, IEquatable<TCategory>
	{
		public bool IsCategory => true;

		public abstract TCategory CategoryKey { get; }
	}
}
