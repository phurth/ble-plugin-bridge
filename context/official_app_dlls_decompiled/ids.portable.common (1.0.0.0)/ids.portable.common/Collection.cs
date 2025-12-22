using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Portable.Common
{
	public static class Collection
	{
		public static bool TryGetValueAtIndex<TItem>(this ICollection<TItem> collection, int index, out TItem item)
		{
			try
			{
				if (index >= collection.Count || index < 0)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				item = Enumerable.ElementAt(collection, index);
				return true;
			}
			catch
			{
				item = default(TItem);
				return false;
			}
		}

		public static bool TryGetValueAtIndex<TItem>(this ICollection<TItem> collection, object intIndexObj, out TItem item)
		{
			if (!(intIndexObj is int index))
			{
				item = default(TItem);
				return false;
			}
			return collection.TryGetValueAtIndex(index, out item);
		}

		public static void TryRemove<TItem>(this ICollection<TItem> collection, TItem item)
		{
			try
			{
				if (collection.Contains(item))
				{
					collection.Remove(item);
				}
			}
			catch
			{
			}
		}

		public static bool HashSetEquals<TValue>(HashSet<TValue> hashSet1, HashSet<TValue> hashSet2)
		{
			if (hashSet1 == hashSet2)
			{
				return true;
			}
			if (hashSet1 == null || hashSet2 == null)
			{
				return false;
			}
			return hashSet1.SetEquals(hashSet2);
		}
	}
}
