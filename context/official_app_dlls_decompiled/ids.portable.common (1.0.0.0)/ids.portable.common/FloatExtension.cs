using System;

namespace IDS.Portable.Common
{
	public static class FloatExtension
	{
		public static bool Equals(this float value1, float value2, float within)
		{
			return Math.Abs(value1 - value2) <= within;
		}
	}
}
