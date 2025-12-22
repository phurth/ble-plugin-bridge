using System.Collections.Concurrent;

namespace IDS.Portable.Common
{
	public static class ConcurrentBagExtension
	{
		public static void Clear<T>(this ConcurrentBag<T> obj)
		{
			T val;
			while (obj.TryTake(out val))
			{
			}
		}
	}
}
