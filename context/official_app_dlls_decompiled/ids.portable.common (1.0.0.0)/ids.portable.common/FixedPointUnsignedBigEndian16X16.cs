using System;

namespace IDS.Portable.Common
{
	public static class FixedPointUnsignedBigEndian16X16
	{
		public const float ScaleFactor = 65536f;

		public static uint ToFixedPoint(byte[] buffer, uint startOffset)
		{
			return (uint)((buffer[startOffset] << 24) | (buffer[1 + startOffset] << 16) | (buffer[2 + startOffset] << 8) | buffer[3 + startOffset]);
		}

		public static uint ToFixedPoint(float realNumber)
		{
			return Convert.ToUInt32(realNumber * 65536f);
		}

		public static float ToFloat(uint fixedPointNumber)
		{
			return (float)fixedPointNumber / 65536f;
		}

		public static float ToFloat(byte[] buffer, uint startOffset)
		{
			return ToFloat(ToFixedPoint(buffer, startOffset));
		}

		public static void FromFloat(byte[] buffer, uint startOffset, float realNumber)
		{
			uint num = ToFixedPoint(realNumber);
			buffer[startOffset] = Convert.ToByte((0xFF000000u & num) >> 24);
			buffer[1 + startOffset] = Convert.ToByte((0xFF0000 & num) >> 16);
			buffer[2 + startOffset] = Convert.ToByte((0xFF00 & num) >> 8);
			buffer[3 + startOffset] = Convert.ToByte(0xFFu & num);
		}
	}
}
