using System;

namespace IDS.Portable.Common
{
	public static class FixedPointSignedBigEndian16X16
	{
		public const float ScaleFactor = 65536f;

		public static int ToFixedPoint(byte[] buffer, uint startOffset)
		{
			return (int)FixedPointUnsignedBigEndian16X16.ToFixedPoint(buffer, startOffset);
		}

		public static int ToFixedPoint(float realNumber)
		{
			return Convert.ToInt32(realNumber * 65536f);
		}

		public static float ToFloat(int fixedPointNumber)
		{
			return (float)fixedPointNumber / 65536f;
		}

		public static float ToFloat(uint fixedPointNumber)
		{
			return (float)(int)fixedPointNumber / 65536f;
		}

		public static float ToFloat(byte[] buffer, uint startOffset)
		{
			return ToFloat(ToFixedPoint(buffer, startOffset));
		}

		public static void FromFloat(byte[] buffer, uint startOffset, float realNumber)
		{
			int num = ToFixedPoint(realNumber);
			buffer[startOffset] = Convert.ToByte((0xFF000000u & num) >> 24);
			buffer[1 + startOffset] = Convert.ToByte((0xFF0000 & num) >> 16);
			buffer[2 + startOffset] = Convert.ToByte((0xFF00 & num) >> 8);
			buffer[3 + startOffset] = Convert.ToByte(0xFF & num);
		}
	}
}
