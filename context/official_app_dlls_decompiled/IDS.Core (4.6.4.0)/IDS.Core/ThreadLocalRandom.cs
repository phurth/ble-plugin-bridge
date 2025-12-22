using System;
using System.Threading;

namespace IDS.Core
{
	public static class ThreadLocalRandom
	{
		private static readonly ThreadSafeRandom globalRandom = new ThreadSafeRandom();

		private static readonly ThreadLocal<Random> threadRandom = new ThreadLocal<Random>(RandomFactory);

		public static Random Instance => threadRandom.Value;

		private static Random RandomFactory()
		{
			return new Random(globalRandom.Next());
		}

		public static int Next()
		{
			return Instance.Next();
		}

		public static int Next(int maxValue)
		{
			return Instance.Next(maxValue);
		}

		public static int Next(int minValue, int maxValue)
		{
			return Instance.Next(minValue, maxValue);
		}

		public static double NextDouble()
		{
			return Instance.NextDouble();
		}

		public static void NextBytes(byte[] buffer)
		{
			Instance.NextBytes(buffer);
		}
	}
}
