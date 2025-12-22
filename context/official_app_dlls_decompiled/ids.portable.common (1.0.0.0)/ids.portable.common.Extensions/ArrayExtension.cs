using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common.Extensions
{
	public static class ArrayExtension
	{
		public enum Endian
		{
			Big,
			Little
		}

		public const int BytesIn48Bits = 6;

		public const int BytesIn64Bits = 8;

		public const int BitsInByte = 8;

		public const int GuidSize = 16;

		public static string DebugDump(this byte[] array, string separator = " ", bool binary = false)
		{
			if (binary)
			{
				return string.Join(separator, Enumerable.Select(array, (byte x) => Convert.ToString(x, 2).PadLeft(8, '0')));
			}
			return BitConverter.ToString(array).Replace("-", separator);
		}

		public static string DebugDump(this IReadOnlyList<byte> array, int startIndex, int length, string separator = " ", bool binary = false)
		{
			if (binary)
			{
				return string.Join(separator, Enumerable.Select(Enumerable.Where(array, (byte x, int index) => index >= startIndex && index < length), (byte x) => Convert.ToString(x, 2).PadLeft(8, '0')));
			}
			return string.Join(separator, Enumerable.Select(Enumerable.Where(array, (byte x, int index) => index >= startIndex && index < length), (byte x) => Convert.ToString(x, 16).PadLeft(2, '0')));
		}

		public static void ResizeIfMoreSpaceNeeded(ref byte[] buffer, int absoluteBitPositionStart, int numBits, int bufferByteOffsetStart = 0)
		{
			int minimumBytesNeededToCopyBitsToArray = GetMinimumBytesNeededToCopyBitsToArray(absoluteBitPositionStart, numBits, bufferByteOffsetStart);
			if (buffer.Length < minimumBytesNeededToCopyBitsToArray)
			{
				Array.Resize(ref buffer, minimumBytesNeededToCopyBitsToArray);
			}
		}

		public static int GetMinimumBytesNeededToCopyBitsToArray(int absoluteBitPositionStart, int numBits, int bufferByteOffsetStart = 0)
		{
			return (absoluteBitPositionStart + numBits - 1) / 8 + 1 + bufferByteOffsetStart;
		}

		public static void CopyBitsToArray(this byte[] buffer, byte data, int absoluteBitPositionStart, int numBits, int bufferByteOffsetStart = 0)
		{
			if (numBits < 1 || numBits > 8)
			{
				throw new ArgumentOutOfRangeException("numBits");
			}
			ushort num = (ushort)((1 << numBits) - 1);
			int num2 = absoluteBitPositionStart / 8;
			int num3 = absoluteBitPositionStart - num2 * 8;
			int num4 = 16 - num3 - numBits;
			ushort num5 = (ushort)((data & num) << num4);
			ushort num6 = (ushort)(num << num4);
			byte b = (byte)(num5 >> 8);
			byte b2 = (byte)(num6 >> 8);
			byte b3 = (byte)(num5 & 0xFFu);
			byte b4 = (byte)(num6 & 0xFFu);
			buffer[bufferByteOffsetStart + num2] = (byte)((buffer[bufferByteOffsetStart + num2] & ~b2) | b);
			if (b4 != 0)
			{
				buffer[bufferByteOffsetStart + num2 + 1] = (byte)((buffer[bufferByteOffsetStart + num2 + 1] & ~b4) | b3);
			}
		}

		public static byte CopyBitsFromArray(this byte[] buffer, int absoluteBitPositionStart, int numBits, int bufferByteOffsetStart = 0)
		{
			if (numBits < 1 || numBits > 8)
			{
				throw new ArgumentOutOfRangeException("numBits");
			}
			ushort num = (ushort)((1 << numBits) - 1);
			int num2 = absoluteBitPositionStart / 8;
			int num3 = absoluteBitPositionStart - num2 * 8;
			int num4 = 16 - num3 - numBits;
			ushort num5 = (ushort)(num << num4);
			byte b = (byte)(num5 >> 8);
			byte b2 = (byte)(num5 & 0xFFu);
			ushort num6 = (ushort)((buffer[bufferByteOffsetStart + num2] & b) << 8);
			if (b2 != 0)
			{
				num6 = (ushort)(num6 | (ushort)(buffer[bufferByteOffsetStart + num2 + 1] & b2));
			}
			return (byte)(num6 >> num4);
		}

		public static byte[] GetBytes(this ushort value, Endian endian = Endian.Big)
		{
			byte[] array = new byte[2];
			switch (endian)
			{
			case Endian.Big:
				array[0] = (byte)((value & 0xFF00) >> 8);
				array[1] = (byte)(value & 0xFFu);
				break;
			case Endian.Little:
				array[1] = (byte)((value & 0xFF00) >> 8);
				array[0] = (byte)(value & 0xFFu);
				break;
			default:
				throw new NotImplementedException();
			}
			return array;
		}

		public static ushort GetValueUInt16(this IReadOnlyList<byte> buffer, int startOffset, Endian endian = Endian.Big)
		{
			byte b;
			byte b2;
			switch (endian)
			{
			case Endian.Big:
				b = buffer[startOffset];
				b2 = buffer[1 + startOffset];
				break;
			case Endian.Little:
				b = buffer[1 + startOffset];
				b2 = buffer[startOffset];
				break;
			default:
				throw new NotImplementedException();
			}
			return (ushort)((b << 8) | b2);
		}

		public static ushort GetValueUInt16(this byte[] buffer, int startOffset, Endian endian = Endian.Big)
		{
			return new ArraySegment<byte>(buffer, startOffset, 2).GetValueUInt16(0, endian);
		}

		public static void SetValueUInt16(this byte[] buffer, ushort value, int startOffset, Endian endian = Endian.Big)
		{
			switch (endian)
			{
			case Endian.Big:
				buffer[startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[1 + startOffset] = (byte)(0xFFu & value);
				break;
			case Endian.Little:
				buffer[1 + startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[startOffset] = (byte)(0xFFu & value);
				break;
			default:
				throw new NotImplementedException();
			}
		}

		public static byte[] GetBytes(this short value, Endian endian = Endian.Big)
		{
			byte[] array = new byte[2];
			switch (endian)
			{
			case Endian.Big:
				array[0] = (byte)((value & 0xFF00) >> 8);
				array[1] = (byte)((uint)value & 0xFFu);
				break;
			case Endian.Little:
				array[1] = (byte)((value & 0xFF00) >> 8);
				array[0] = (byte)((uint)value & 0xFFu);
				break;
			default:
				throw new NotImplementedException();
			}
			return array;
		}

		public static short GetValueInt16(this IReadOnlyList<byte> buffer, int startOffset, Endian endian = Endian.Big)
		{
			byte b;
			byte b2;
			switch (endian)
			{
			case Endian.Big:
				b = buffer[startOffset];
				b2 = buffer[1 + startOffset];
				break;
			case Endian.Little:
				b = buffer[1 + startOffset];
				b2 = buffer[startOffset];
				break;
			default:
				throw new NotImplementedException();
			}
			return (short)((b << 8) | b2);
		}

		public static short GetValueInt16(this byte[] buffer, int startOffset, Endian endian = Endian.Big)
		{
			return new ArraySegment<byte>(buffer, startOffset, 2).GetValueInt16(0, endian);
		}

		public static void SetValueInt16(this byte[] buffer, short value, int startOffset, Endian endian = Endian.Big)
		{
			switch (endian)
			{
			case Endian.Big:
				buffer[startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[1 + startOffset] = (byte)(0xFFu & (uint)value);
				break;
			case Endian.Little:
				buffer[1 + startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[startOffset] = (byte)(0xFFu & (uint)value);
				break;
			default:
				throw new NotImplementedException();
			}
		}

		public static byte[] GetBytes(this uint value, Endian endian = Endian.Big)
		{
			byte[] array = new byte[4];
			switch (endian)
			{
			case Endian.Big:
				array[0] = (byte)((value & 0xFF000000u) >> 24);
				array[1] = (byte)((value & 0xFF0000) >> 16);
				array[2] = (byte)((value & 0xFF00) >> 8);
				array[3] = (byte)(value & 0xFFu);
				break;
			case Endian.Little:
				array[3] = (byte)((value & 0xFF000000u) >> 24);
				array[2] = (byte)((value & 0xFF0000) >> 16);
				array[1] = (byte)((value & 0xFF00) >> 8);
				array[0] = (byte)(value & 0xFFu);
				break;
			default:
				throw new NotImplementedException();
			}
			return array;
		}

		public static uint GetValueUInt32(this IReadOnlyList<byte> buffer, int startOffset, Endian endian = Endian.Big)
		{
			byte b;
			byte b2;
			byte b3;
			byte b4;
			switch (endian)
			{
			case Endian.Big:
				b = buffer[startOffset];
				b2 = buffer[1 + startOffset];
				b3 = buffer[2 + startOffset];
				b4 = buffer[3 + startOffset];
				break;
			case Endian.Little:
				b = buffer[3 + startOffset];
				b2 = buffer[2 + startOffset];
				b3 = buffer[1 + startOffset];
				b4 = buffer[startOffset];
				break;
			default:
				throw new NotImplementedException();
			}
			return (uint)((b << 24) | (b2 << 16) | (b3 << 8) | b4);
		}

		public static uint GetValueUInt32(this byte[] buffer, int startOffset, Endian endian = Endian.Big)
		{
			return new ArraySegment<byte>(buffer, startOffset, 4).GetValueUInt32(0, endian);
		}

		public static void SetValueUInt32(this byte[] buffer, uint value, int startOffset, Endian endian = Endian.Big)
		{
			switch (endian)
			{
			case Endian.Big:
				buffer[startOffset] = (byte)((0xFF000000u & value) >> 24);
				buffer[1 + startOffset] = (byte)((0xFF0000 & value) >> 16);
				buffer[2 + startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[3 + startOffset] = (byte)(0xFFu & value);
				break;
			case Endian.Little:
				buffer[3 + startOffset] = (byte)((0xFF000000u & value) >> 24);
				buffer[2 + startOffset] = (byte)((0xFF0000 & value) >> 16);
				buffer[1 + startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[startOffset] = (byte)(0xFFu & value);
				break;
			default:
				throw new NotImplementedException();
			}
		}

		public static byte[] GetBytes(this int value, Endian endian = Endian.Big)
		{
			byte[] array = new byte[4];
			switch (endian)
			{
			case Endian.Big:
				array[0] = (byte)((value & 0xFF000000u) >> 24);
				array[1] = (byte)((value & 0xFF0000) >> 16);
				array[2] = (byte)((value & 0xFF00) >> 8);
				array[3] = (byte)((uint)value & 0xFFu);
				break;
			case Endian.Little:
				array[3] = (byte)((value & 0xFF000000u) >> 24);
				array[2] = (byte)((value & 0xFF0000) >> 16);
				array[1] = (byte)((value & 0xFF00) >> 8);
				array[0] = (byte)((uint)value & 0xFFu);
				break;
			default:
				throw new NotImplementedException();
			}
			return array;
		}

		public static int GetValueInt32(this IReadOnlyList<byte> buffer, int startOffset, Endian endian = Endian.Big)
		{
			byte b;
			byte b2;
			byte b3;
			byte b4;
			switch (endian)
			{
			case Endian.Big:
				b = buffer[startOffset];
				b2 = buffer[1 + startOffset];
				b3 = buffer[2 + startOffset];
				b4 = buffer[3 + startOffset];
				break;
			case Endian.Little:
				b = buffer[3 + startOffset];
				b2 = buffer[2 + startOffset];
				b3 = buffer[1 + startOffset];
				b4 = buffer[startOffset];
				break;
			default:
				throw new NotImplementedException();
			}
			return (b << 24) | (b2 << 16) | (b3 << 8) | b4;
		}

		public static int GetValueInt32(this byte[] buffer, int startOffset, Endian endian = Endian.Big)
		{
			return new ArraySegment<byte>(buffer, startOffset, 4).GetValueInt32(0, endian);
		}

		public static void SetValueInt32(this byte[] buffer, int value, int startOffset, Endian endian = Endian.Big)
		{
			switch (endian)
			{
			case Endian.Big:
				buffer[startOffset] = (byte)((0xFF000000u & value) >> 24);
				buffer[1 + startOffset] = (byte)((0xFF0000 & value) >> 16);
				buffer[2 + startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[3 + startOffset] = (byte)(0xFFu & (uint)value);
				break;
			case Endian.Little:
				buffer[3 + startOffset] = (byte)((0xFF000000u & value) >> 24);
				buffer[2 + startOffset] = (byte)((0xFF0000 & value) >> 16);
				buffer[1 + startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[startOffset] = (byte)(0xFFu & (uint)value);
				break;
			default:
				throw new NotImplementedException();
			}
		}

		public static void SetValueUInt24(this byte[] buffer, uint value, int startOffset, Endian endian = Endian.Big)
		{
			switch (endian)
			{
			case Endian.Big:
				buffer[startOffset] = (byte)((0xFF0000 & value) >> 16);
				buffer[1 + startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[2 + startOffset] = (byte)(0xFFu & value);
				break;
			case Endian.Little:
				buffer[2 + startOffset] = (byte)((0xFF0000 & value) >> 16);
				buffer[1 + startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[startOffset] = (byte)(0xFFu & value);
				break;
			default:
				throw new NotImplementedException();
			}
		}

		public static ulong GetValueUInt48(this IReadOnlyList<byte> buffer, int startOffset, Endian endian = Endian.Big)
		{
			ulong num;
			ulong num2;
			ulong num3;
			ulong num4;
			ulong num5;
			ulong num6;
			switch (endian)
			{
			case Endian.Big:
				num = buffer[startOffset];
				num2 = buffer[1 + startOffset];
				num3 = buffer[2 + startOffset];
				num4 = buffer[3 + startOffset];
				num5 = buffer[4 + startOffset];
				num6 = buffer[5 + startOffset];
				break;
			case Endian.Little:
				num = buffer[5 + startOffset];
				num2 = buffer[4 + startOffset];
				num3 = buffer[3 + startOffset];
				num4 = buffer[2 + startOffset];
				num5 = buffer[1 + startOffset];
				num6 = buffer[startOffset];
				break;
			default:
				throw new NotImplementedException();
			}
			return (num << 40) | (num2 << 32) | (num3 << 24) | (num4 << 16) | (num5 << 8) | num6;
		}

		public static ulong GetValueUInt48(this byte[] buffer, int startOffset, Endian endian = Endian.Big)
		{
			return new ArraySegment<byte>(buffer, startOffset, 6).GetValueUInt48(0, endian);
		}

		public static void SetValueUInt48(this byte[] buffer, ulong value, int startOffset, Endian endian = Endian.Big)
		{
			switch (endian)
			{
			case Endian.Big:
				buffer[startOffset] = (byte)((0xFF0000000000L & value) >> 40);
				buffer[1 + startOffset] = (byte)((0xFF00000000L & value) >> 32);
				buffer[2 + startOffset] = (byte)((0xFF000000u & value) >> 24);
				buffer[3 + startOffset] = (byte)((0xFF0000 & value) >> 16);
				buffer[4 + startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[5 + startOffset] = (byte)(0xFF & value);
				break;
			case Endian.Little:
				buffer[5 + startOffset] = (byte)((0xFF0000000000L & value) >> 40);
				buffer[4 + startOffset] = (byte)((0xFF00000000L & value) >> 32);
				buffer[3 + startOffset] = (byte)((0xFF000000u & value) >> 24);
				buffer[2 + startOffset] = (byte)((0xFF0000 & value) >> 16);
				buffer[1 + startOffset] = (byte)((0xFF00 & value) >> 8);
				buffer[startOffset] = (byte)(0xFF & value);
				break;
			default:
				throw new NotImplementedException();
			}
		}

		public static ulong GetValueUInt64(this IReadOnlyList<byte> buffer, int startOffset, Endian endian = Endian.Big)
		{
			int num = 8;
			ulong num2 = 0uL;
			int num3 = 0;
			switch (endian)
			{
			case Endian.Big:
			{
				int num5 = num - 1;
				while (num5 >= 0)
				{
					num2 |= (ulong)buffer[num5 + startOffset] << num3;
					num5--;
					num3 += 8;
				}
				break;
			}
			case Endian.Little:
			{
				int num4 = 0;
				while (num4 < num)
				{
					num2 |= (ulong)buffer[num4 + startOffset] << num3;
					num4++;
					num3 += 8;
				}
				break;
			}
			default:
				throw new NotImplementedException();
			}
			return num2;
		}

		public static ulong GetValueUInt64(this byte[] buffer, int startOffset, Endian endian = Endian.Big)
		{
			return new ArraySegment<byte>(buffer, startOffset, 8).GetValueUInt48(0, endian);
		}

		public static void SetValueUInt64(this byte[] buffer, ulong value, int startOffset, Endian endian = Endian.Big)
		{
			int num = 8;
			ulong num2 = 255uL;
			int num3 = 0;
			switch (endian)
			{
			case Endian.Big:
			{
				int num5 = num - 1;
				while (num5 >= 0)
				{
					buffer[num5 + startOffset] = (byte)((value & num2) >> num3);
					num5--;
					num3 += 8;
					num2 <<= 8;
				}
				break;
			}
			case Endian.Little:
			{
				int num4 = num - 1;
				while (num4 >= 0)
				{
					buffer[num4 + startOffset] = (byte)((value & num2) >> num3);
					num4--;
					num3 += 8;
					num2 <<= 8;
				}
				break;
			}
			default:
				throw new NotImplementedException();
			}
		}

		public static int SetValueBigEndianRemoveLeadingZeros(this byte[] buffer, ulong value, int startOffset, int numBytes)
		{
			if (numBytes == 0)
			{
				throw new ArgumentOutOfRangeException("numBytes", numBytes, "must be less greater then 0");
			}
			if (numBytes > 8)
			{
				object obj = numBytes;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 1);
				defaultInterpolatedStringHandler.AppendLiteral("must be less then or equal to ");
				defaultInterpolatedStringHandler.AppendFormatted(8);
				throw new ArgumentOutOfRangeException("numBytes", obj, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (numBytes > buffer.Length)
			{
				object obj2 = numBytes;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(57, 1);
				defaultInterpolatedStringHandler.AppendLiteral("must be less then or equal to the given buffer length of ");
				defaultInterpolatedStringHandler.AppendFormatted(buffer.Length);
				throw new ArgumentOutOfRangeException("numBytes", obj2, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			ulong num = (ulong)(255L << (numBytes - 1) * 8);
			int num2 = 0;
			int num3 = 0;
			int num4 = (numBytes - 1) * 8;
			while (num4 >= 0)
			{
				ulong num5 = value & num;
				if (num5 == 0L && num3 == 0)
				{
					num2++;
				}
				else
				{
					buffer[num3 + startOffset] |= (byte)(num5 >> num4);
					num3++;
				}
				num4 -= 8;
				num >>= 8;
			}
			return numBytes - num2;
		}

		public static Guid ToGuid(this byte[] values, int startOffset, Endian endian = Endian.Big)
		{
			if (startOffset + 16 > values.Length)
			{
				throw new ArgumentException("Array too small to read full GUID", "values");
			}
			int num = startOffset;
			int valueUInt = (int)values.GetValueUInt32(num, endian);
			num += 4;
			short b = (short)values.GetValueUInt16(num, endian);
			num += 2;
			short c = (short)values.GetValueUInt16(num, endian);
			num += 2;
			byte[] array = new byte[8];
			for (int i = 0; i < 8; i++)
			{
				array[i] = values[num++];
			}
			return new Guid(valueUInt, b, c, array);
		}

		public static void SetFixedPointFloat(this byte[] buffer, float value, uint startOffset, FixedPointType fixedPoint)
		{
			fixedPoint.SetFixedPointFloat(buffer, startOffset, value);
		}

		public static float GetFixedPointFloat(this byte[] buffer, uint startOffset, FixedPointType fixedPoint)
		{
			return fixedPoint.GetFixedPointFloat(buffer, startOffset);
		}

		public static void Clear<TValue>(this TValue[] buffer)
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				buffer[i] = default(TValue);
			}
		}
	}
}
