using System.Collections.Generic;

namespace IDS.Portable.Common
{
	public static class ArrayCommon
	{
		public static bool ArraysEqual<T>(T[] a1, T[] a2)
		{
			if (a1 == a2)
			{
				return true;
			}
			if (a1 == null || a2 == null)
			{
				return false;
			}
			if (a1.Length != a2.Length)
			{
				return false;
			}
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			for (int i = 0; i < a1.Length; i++)
			{
				if (!@default.Equals(a1[i], a2[i]))
				{
					return false;
				}
			}
			return true;
		}

		public static bool ArraysEqual<T>(T[] a1, T[] a2, int size)
		{
			if (a1 == a2)
			{
				return true;
			}
			if (a1 == null || a2 == null)
			{
				return false;
			}
			if (a1.Length < size || a2.Length < size)
			{
				return false;
			}
			EqualityComparer<T> @default = EqualityComparer<T>.Default;
			for (int i = 0; i < size; i++)
			{
				if (!@default.Equals(a1[i], a2[i]))
				{
					return false;
				}
			}
			return true;
		}
	}
}
