using System;

namespace IDS.Portable.Common
{
	public static class FixedPointSignedBigEndian8X8
	{
		public const float ScaleFactor = 256f;

		public static short ToFixedPoint(byte[] buffer, uint startOffset)
		{
			return (short)FixedPointUnsignedBigEndian8X8.ToFixedPoint(buffer, startOffset);
		}

		public static short ToFixedPoint(float realNumber)
		{
			return Convert.ToInt16(realNumber * 256f);
		}

		public static float ToFloat(short fixedPointNumber)
		{
			return (float)fixedPointNumber / 256f;
		}

		public static float ToFloat(ushort fixedPointNumber)
		{
			return (float)(short)fixedPointNumber / 256f;
		}

		public static float ToFloat(byte[] buffer, uint startOffset)
		{
			return ToFloat(ToFixedPoint(buffer, startOffset));
		}

		public static void FromFloat(byte[] buffer, uint startOffset, float realNumber)
		{
			short num = ToFixedPoint(realNumber);
			buffer[startOffset] = Convert.ToByte((0xFF00 & num) >> 8);
			buffer[1 + startOffset] = Convert.ToByte(0xFF & num);
		}
	}
}
