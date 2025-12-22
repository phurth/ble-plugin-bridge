using System;

namespace IDS.Portable.Common
{
	public static class FixedPointUnsignedBigEndian8X8
	{
		public const float ScaleFactor = 256f;

		public static ushort ToFixedPoint(byte[] buffer, uint startOffset)
		{
			return (ushort)((ushort)(buffer[startOffset] << 8) | buffer[1 + startOffset]);
		}

		public static ushort ToFixedPoint(float realNumber)
		{
			return Convert.ToUInt16(realNumber * 256f);
		}

		public static float ToFloat(ushort fixedPointNumber)
		{
			return (float)(int)fixedPointNumber / 256f;
		}

		public static float ToFloat(byte[] buffer, uint startOffset)
		{
			return ToFloat(ToFixedPoint(buffer, startOffset));
		}

		public static void FromFloat(byte[] buffer, uint startOffset, float realNumber)
		{
			ushort num = ToFixedPoint(realNumber);
			buffer[startOffset] = Convert.ToByte((0xFF00 & num) >> 8);
			buffer[1 + startOffset] = Convert.ToByte(0xFF & num);
		}
	}
}
