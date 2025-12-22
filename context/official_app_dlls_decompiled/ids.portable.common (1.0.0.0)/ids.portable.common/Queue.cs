using System.Collections.Generic;

namespace IDS.Portable.Common
{
	public static class Queue
	{
		public static bool TryDequeue<TValue>(this Queue<TValue> queue, out TValue value)
		{
			try
			{
				if (queue.Count == 0)
				{
					value = default(TValue);
					return false;
				}
				value = queue.Dequeue();
				return true;
			}
			catch
			{
				value = default(TValue);
				return false;
			}
		}
	}
}
