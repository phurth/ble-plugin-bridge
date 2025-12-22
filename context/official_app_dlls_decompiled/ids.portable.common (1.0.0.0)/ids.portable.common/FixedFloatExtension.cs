using System;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common
{
	public static class FixedFloatExtension
	{
		public static void SetFixedPointFloat(this FixedPointType fixedPointType, byte[] buffer, uint startOffset, float realNumber)
		{
			switch (fixedPointType)
			{
			case FixedPointType.UnsignedBigEndian8x8:
				FixedPointUnsignedBigEndian8X8.FromFloat(buffer, startOffset, realNumber);
				return;
			case FixedPointType.UnsignedBigEndian16x16:
				FixedPointUnsignedBigEndian16X16.FromFloat(buffer, startOffset, realNumber);
				return;
			case FixedPointType.SignedBigEndian8x8:
				FixedPointSignedBigEndian8X8.FromFloat(buffer, startOffset, realNumber);
				return;
			case FixedPointType.SignedBigEndian16x16:
				FixedPointSignedBigEndian16X16.FromFloat(buffer, startOffset, realNumber);
				return;
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Unknown ");
			defaultInterpolatedStringHandler.AppendFormatted("FixedPointType");
			defaultInterpolatedStringHandler.AppendLiteral(" of ");
			defaultInterpolatedStringHandler.AppendFormatted(fixedPointType);
			throw new NotImplementedException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public static float GetFixedPointFloat(this FixedPointType fixedPointType, byte[] buffer, uint startOffset)
		{
			switch (fixedPointType)
			{
			case FixedPointType.UnsignedBigEndian8x8:
				return FixedPointUnsignedBigEndian8X8.ToFloat(buffer, startOffset);
			case FixedPointType.UnsignedBigEndian16x16:
				return FixedPointUnsignedBigEndian16X16.ToFloat(buffer, startOffset);
			case FixedPointType.SignedBigEndian8x8:
				return FixedPointSignedBigEndian8X8.ToFloat(buffer, startOffset);
			case FixedPointType.SignedBigEndian16x16:
				return FixedPointSignedBigEndian16X16.ToFloat(buffer, startOffset);
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unknown ");
				defaultInterpolatedStringHandler.AppendFormatted("fixedPointType");
				defaultInterpolatedStringHandler.AppendLiteral(" of ");
				defaultInterpolatedStringHandler.AppendFormatted(fixedPointType);
				throw new NotImplementedException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
		}

		public static float FixedPointToFloat(this FixedPointType fixedPointType, ulong fixedPointNumber)
		{
			switch (fixedPointType)
			{
			case FixedPointType.UnsignedBigEndian8x8:
				return FixedPointUnsignedBigEndian8X8.ToFloat((ushort)(fixedPointNumber & 0xFFFF));
			case FixedPointType.UnsignedBigEndian16x16:
				return FixedPointUnsignedBigEndian16X16.ToFloat((uint)(fixedPointNumber & 0xFFFFFFFFu));
			case FixedPointType.SignedBigEndian8x8:
				return FixedPointSignedBigEndian8X8.ToFloat((short)(fixedPointNumber & 0xFFFF));
			case FixedPointType.SignedBigEndian16x16:
				return FixedPointSignedBigEndian16X16.ToFloat((int)(fixedPointNumber & 0xFFFFFFFFu));
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unknown ");
				defaultInterpolatedStringHandler.AppendFormatted("fixedPointType");
				defaultInterpolatedStringHandler.AppendLiteral(" of ");
				defaultInterpolatedStringHandler.AppendFormatted(fixedPointType);
				throw new NotImplementedException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
		}

		public static ulong ToFixedPointAsULong(this FixedPointType fixedPointType, float realNumber)
		{
			switch (fixedPointType)
			{
			case FixedPointType.UnsignedBigEndian8x8:
				return FixedPointUnsignedBigEndian8X8.ToFixedPoint(realNumber);
			case FixedPointType.UnsignedBigEndian16x16:
				return FixedPointUnsignedBigEndian16X16.ToFixedPoint(realNumber);
			case FixedPointType.SignedBigEndian8x8:
				return (ulong)FixedPointSignedBigEndian8X8.ToFixedPoint(realNumber);
			case FixedPointType.SignedBigEndian16x16:
				return (ulong)FixedPointSignedBigEndian16X16.ToFixedPoint(realNumber);
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unknown ");
				defaultInterpolatedStringHandler.AppendFormatted("fixedPointType");
				defaultInterpolatedStringHandler.AppendLiteral(" of ");
				defaultInterpolatedStringHandler.AppendFormatted(fixedPointType);
				throw new NotImplementedException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
		}
	}
}
