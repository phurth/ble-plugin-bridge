using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Portable.Common.Extensions
{
	public static class IList
	{
		public static TItem[] Slice<TItem>(this TItem[] instance, int startIndex)
		{
			if (startIndex < 0 || startIndex >= instance.Length)
			{
				throw new ArgumentOutOfRangeException("startIndex", "startIndex is less than zero or greater than the length of this instance");
			}
			return instance.Slice(startIndex, instance.Length - startIndex);
		}

		public static TItem[] Slice<TItem>(this TItem[] instance, int startIndex, int length)
		{
			if (startIndex + length > instance.Length)
			{
				throw new ArgumentException("startIndex plus length indicates a position not within this instance");
			}
			if (startIndex < 0 || length < 0)
			{
				throw new ArgumentException("startIndex or length is less than zero");
			}
			TItem[] array = new TItem[length];
			for (int i = 0; i < length; i++)
			{
				array[i] = instance[i + startIndex];
			}
			return array;
		}

		public static bool EqualsAll<T>(this IList<T>? a, IList<T>? b)
		{
			if (a == null || b == null)
			{
				if (a == null)
				{
					return b == null;
				}
				return false;
			}
			if (a!.Count == b!.Count)
			{
				return Enumerable.All(a, b!.Contains);
			}
			return false;
		}

		public static bool TryTakeFirst<T>(this IList<T> list, out T? item)
		{
			if (list.Count == 0)
			{
				item = default(T);
				return false;
			}
			item = list[0];
			list.RemoveAt(0);
			return true;
		}
	}
}
