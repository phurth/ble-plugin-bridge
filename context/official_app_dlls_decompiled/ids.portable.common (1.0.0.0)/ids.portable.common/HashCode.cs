using System.Collections.Generic;

namespace IDS.Portable.Common
{
	public static class HashCode
	{
		public const int Start = 17;

		public static int Hash<T>(this int hash, T obj)
		{
			int hashCode = EqualityComparer<T>.Default.GetHashCode(obj);
			return CalculateHash(hash, hashCode);
		}

		public static int Hash<T>(this int hash, T[] objectArray)
		{
			if (objectArray == null)
			{
				return hash;
			}
			for (int i = 0; i < objectArray.Length; i++)
			{
				int hashCode = EqualityComparer<T>.Default.GetHashCode(objectArray[i]);
				hash = CalculateHash(hash, hashCode);
			}
			return hash;
		}

		public static int Hash(this int hash, byte[] byteArray)
		{
			if (byteArray == null)
			{
				return hash;
			}
			for (int i = 0; i < byteArray.Length; i++)
			{
				hash = CalculateHash(hash, byteArray[i]);
			}
			return hash;
		}

		private static int CalculateHash(int hash, int hashObject)
		{
			return hash * 31 + hashObject;
		}
	}
}
