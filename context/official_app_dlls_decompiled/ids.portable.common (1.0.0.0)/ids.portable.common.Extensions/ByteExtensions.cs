using System;

namespace IDS.Portable.Common.Extensions
{
	public static class ByteExtensions
	{
		public static string ToBinaryString(this byte b)
		{
			return Convert.ToString(b, 2).PadLeft(8, '0');
		}

		public static bool IsBitSet(this byte data, int offset)
		{
			if ((offset < 0 || offset > 7) ? true : false)
			{
				throw new ArgumentOutOfRangeException("offset", "Value must be between 0 and 7");
			}
			return (data & (1 << offset)) != 0;
		}

		public static void SetBit(this ref byte data, int offset, bool set)
		{
			if ((offset < 0 || offset > 7) ? true : false)
			{
				throw new ArgumentOutOfRangeException("offset", "Value must be between 0 and 7");
			}
			if (set)
			{
				data |= (byte)(1 << offset);
			}
			else
			{
				data &= (byte)(~(1 << offset));
			}
		}

		public static void SetBits(this ref byte dst, int dstOffset, byte src, int srcOffset, int count)
		{
			if ((dstOffset < 0 || dstOffset > 7) ? true : false)
			{
				throw new ArgumentOutOfRangeException("dstOffset", "Value must be between 0 and 7");
			}
			if ((srcOffset < 0 || srcOffset > 7) ? true : false)
			{
				throw new ArgumentOutOfRangeException("srcOffset", "Value must be between 0 and 7");
			}
			if (count < 1 || count > dstOffset + 1 || count > srcOffset + 1)
			{
				throw new ArgumentOutOfRangeException("count", "Value must be between 0 and not greater than srcOffset + 1 or dstOffset + 1");
			}
			byte b = (byte)(~GetBitMask(dstOffset, count));
			byte bitMask = GetBitMask(srcOffset, count);
			if (dstOffset > srcOffset)
			{
				dst = (byte)((dst & b) | ((src & bitMask) << dstOffset - srcOffset));
			}
			else
			{
				dst = (byte)((dst & b) | ((src & bitMask) << srcOffset - dstOffset));
			}
		}

		public static byte GetBits(this byte data, int offset, int count)
		{
			if ((offset < 0 || offset > 7) ? true : false)
			{
				throw new ArgumentOutOfRangeException("offset", "Value must be between 0 and 7");
			}
			if (count < 1 || count > offset + 1)
			{
				throw new ArgumentOutOfRangeException("count", "Value must be between 0 and offset + 1");
			}
			byte bitMask = GetBitMask(offset, count);
			return (byte)((data & bitMask) >> offset + 1 - count);
		}

		private static byte GetBitMask(int offset, int count)
		{
			if ((offset < 0 || offset > 7) ? true : false)
			{
				throw new ArgumentOutOfRangeException("offset", "Value must be between 0 and 7");
			}
			if (count < 1 || count > offset + 1)
			{
				throw new ArgumentOutOfRangeException("count", "Value must be between 0 and offset + 1");
			}
			byte b = 0;
			for (int num = offset; num > offset - count; num--)
			{
				b = (byte)(b | (byte)Math.Pow(2.0, num));
			}
			return b;
		}

		public static byte GetUpperNibble(this byte data)
		{
			return (byte)((data & 0xF0) >> 4);
		}

		public static byte GetLowerNibble(this byte data)
		{
			return (byte)(data & 0xFu);
		}

		[Obsolete("Use GetUpperNibble or GetLowerNibble")]
		public static byte GetNibble(this byte data, int nibbleNumber)
		{
			if ((nibbleNumber < 0 || nibbleNumber > 1) ? true : false)
			{
				throw new ArgumentException("NibbleNumber must be between 0 and 1");
			}
			if (nibbleNumber != 0)
			{
				return (byte)((data & 0xF0) >> 4);
			}
			return (byte)(data & 0xFu);
		}

		public static uint GetUInt32(this ulong data, int byteOffset)
		{
			if (byteOffset != 0)
			{
				return (uint)(data >> byteOffset * 8);
			}
			return (uint)data;
		}

		public static void SetUInt32(this ref ulong data, int byteOffset, uint value)
		{
			ulong num = 4294967295uL << byteOffset * 8;
			ulong num2 = (ulong)value << byteOffset * 8;
			data = (data & ~num) | num2;
		}

		public static ushort GetUInt16(this ulong data, int byteOffset)
		{
			if (byteOffset != 0)
			{
				return (ushort)(data >> byteOffset * 8);
			}
			return (ushort)data;
		}

		public static void SetUInt16(this ref ulong data, int byteOffset, ushort value)
		{
			ulong num = (ulong)(65535L << byteOffset * 8);
			ulong num2 = (ulong)value << byteOffset * 8;
			data = (data & ~num) | num2;
		}

		public static byte GetByte(this ulong data, int byteOffset)
		{
			if (byteOffset != 0)
			{
				return (byte)(data >> byteOffset * 8);
			}
			return (byte)data;
		}

		public static void SetByte(this ref ulong data, int byteOffset, byte value)
		{
			ulong num = (ulong)(255L << byteOffset * 8);
			ulong num2 = (ulong)value << byteOffset * 8;
			data = (data & ~num) | num2;
		}

		[Obsolete("Use GetBits")]
		public static int GetHalfNibble(this byte data, int position)
		{
			if ((position < 0 || position > 2) ? true : false)
			{
				throw new ArgumentException("Position must be between 0 and 1.");
			}
			return 3 & (data >> position * 2);
		}
	}
}
