using System.Collections.Generic;

namespace IDS.Portable.Common.Extensions
{
	public static class ReadOnlyList
	{
		public static void ToExistingArray<TValue>(this IReadOnlyList<TValue> source, TValue[] destination)
		{
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				destination[i] = source[i];
			}
		}

		public static void ToExistingArray<TValue>(this IReadOnlyList<TValue> source, TValue[] destination, int destinationOffset)
		{
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				destination[i + destinationOffset] = source[i];
			}
		}

		public static void ToExistingArray<TValue>(this IReadOnlyList<TValue> source, int sourceOffset, TValue[] destination, int destinationOffset, int count)
		{
			for (int i = 0; i < count; i++)
			{
				destination[i + destinationOffset] = source[i + sourceOffset];
			}
		}

		public static TValue[] ToNewArray<TValue>(this IReadOnlyList<TValue> source, int sourceOffset, int count)
		{
			TValue[] array = new TValue[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = source[i + sourceOffset];
			}
			return array;
		}
	}
}
