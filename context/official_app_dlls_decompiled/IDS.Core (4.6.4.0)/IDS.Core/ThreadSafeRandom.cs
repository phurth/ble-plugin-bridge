using System;

namespace IDS.Core
{
	internal class ThreadSafeRandom
	{
		private readonly Random RNG;

		public ThreadSafeRandom()
		{
			RNG = new Random();
		}

		public ThreadSafeRandom(int seed)
		{
			RNG = new Random(seed);
		}

		public int Next()
		{
			lock (RNG)
			{
				return RNG.Next();
			}
		}

		public int Next(int maxValue)
		{
			lock (RNG)
			{
				return RNG.Next(maxValue);
			}
		}

		public int Next(int minValue, int maxValue)
		{
			lock (RNG)
			{
				return RNG.Next(minValue, maxValue);
			}
		}

		public void NextBytes(byte[] buffer)
		{
			lock (RNG)
			{
				RNG.NextBytes(buffer);
			}
		}

		public double NextDouble()
		{
			lock (RNG)
			{
				return RNG.NextDouble();
			}
		}
	}
}
