using System;
using System.Collections.Generic;

namespace IDS.Portable.Common
{
	public class CustomCompare<T> : IComparer<T>
	{
		private Func<T, T, int> _comparer;

		public CustomCompare(Func<T, T, int> comparer)
		{
			_comparer = comparer;
		}

		public int Compare(T x, T y)
		{
			return _comparer(x, y);
		}
	}
}
