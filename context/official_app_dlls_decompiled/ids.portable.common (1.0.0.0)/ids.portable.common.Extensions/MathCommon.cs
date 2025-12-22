using System;
using System.Collections.Generic;

namespace IDS.Portable.Common.Extensions
{
	public static class MathCommon
	{
		public enum RoundingDirection
		{
			Up,
			Down
		}

		public const double AlwaysRoundUpThreshold = 0.0;

		public const double AlwaysRoundDownThreshold = 1.0;

		public static uint Clamp(uint value, uint min, uint max)
		{
			if (value >= min)
			{
				if (value <= max)
				{
					return value;
				}
				return max;
			}
			return min;
		}

		public static int Clamp(int value, int min, int max)
		{
			if (value >= min)
			{
				if (value <= max)
				{
					return value;
				}
				return max;
			}
			return min;
		}

		public static float Clamp(float value, float min, float max)
		{
			if (!(value < min))
			{
				if (!(value > max))
				{
					return value;
				}
				return max;
			}
			return min;
		}

		public static double Clamp(double value, double min, double max)
		{
			if (!(value < min))
			{
				if (!(value > max))
				{
					return value;
				}
				return max;
			}
			return min;
		}

		public static int RoundUsingThreshold(this double value, double threshold = 0.5)
		{
			double num = Math.Floor(value);
			if (!(value - num >= threshold))
			{
				return (int)num;
			}
			return (int)num + 1;
		}

		public static TValue Snap<TValue>(TValue value, IEnumerable<TValue>? orderedSnapValues, decimal roundUpThreshold = 0.5m) where TValue : struct, IConvertible
		{
			if (orderedSnapValues == null)
			{
				return value;
			}
			decimal valueDec = Convert.ToDecimal(value);
			TValue? val = null;
			TValue? val2 = null;
			foreach (TValue item in orderedSnapValues!)
			{
				if (!val.HasValue)
				{
					val = item;
					continue;
				}
				if (val2.HasValue)
				{
					val = val2;
				}
				val2 = item;
				TValue? val3 = SnapTo(val.Value, val2.Value);
				if (!val3.HasValue)
				{
					continue;
				}
				return val3.Value;
			}
			if (!val.HasValue)
			{
				return value;
			}
			if (!val2.HasValue)
			{
				return val.Value;
			}
			return val2.Value;
			TValue? SnapTo(TValue low, TValue high)
			{
				decimal num = Convert.ToDecimal(low);
				decimal num2 = Convert.ToDecimal(high) - num;
				if (num2 < 0m)
				{
					throw new ArgumentException("Invalid orderedSnapValues, values must be sorted from lowest to highest.");
				}
				if (num2 == 0m && valueDec == num)
				{
					return low;
				}
				decimal num3 = (valueDec - num) / num2;
				if (num3 < 0m)
				{
					return null;
				}
				if (num3 > 1m)
				{
					return null;
				}
				return (num3 >= roundUpThreshold) ? high : low;
			}
		}

		public static double DegreeToRadian(double angle)
		{
			return Math.PI * angle / 180.0;
		}

		public static double RoundToNearest(double value, double roundTo, RoundingDirection roundingDirection)
		{
			if (roundingDirection != 0)
			{
				return RoundDownToNearest(value, roundTo);
			}
			return RoundUpToNearest(value, roundTo);
		}

		public static double RoundUpToNearest(double value, double roundTo)
		{
			if (roundTo <= 0.0)
			{
				throw new ArgumentOutOfRangeException("roundTo", "roundTo must be greater than 0");
			}
			return Math.Ceiling(value / roundTo) * roundTo;
		}

		public static double RoundDownToNearest(double value, double roundTo)
		{
			if (roundTo <= 0.0)
			{
				throw new ArgumentOutOfRangeException("roundTo", "roundTo must be greater than 0");
			}
			return Math.Floor(value / roundTo) * roundTo;
		}

		public static double LinearlyInterpolate(double percentage, double start, double end)
		{
			double num = Clamp(percentage, 0.0, 1.0);
			double num2 = end - start;
			return num * num2 + start;
		}

		public static double InverseLinearlyInterpolate(double lerpValue, double start, double end)
		{
			double num = Clamp(lerpValue, Math.Min(start, end), Math.Max(start, end));
			double num2 = end - start;
			return (num - start) / num2;
		}
	}
}
