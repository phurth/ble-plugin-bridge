using System;
using System.Collections.ObjectModel;

namespace IDS.Portable.Common.Extensions
{
	public static class ObservableCollectionExtensions
	{
		public static void RemoveAll<T>(this ObservableCollection<T> collection, Func<T, bool> condition)
		{
			for (int num = collection.Count - 1; num >= 0; num--)
			{
				if (condition(collection[num]))
				{
					collection.RemoveAt(num);
				}
			}
		}
	}
}
