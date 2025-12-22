using System;

namespace IDS.Portable.Common.ObservableCollection
{
	public interface ICategory<out TCategory> where TCategory : IComparable<TCategory>, IEquatable<TCategory>
	{
		TCategory CategoryKey { get; }
	}
}
