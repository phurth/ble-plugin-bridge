using System;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common
{
	public readonly struct BitPositionValue64
	{
		public readonly int NumBits;

		public readonly int StartBitIndex;

		public readonly int StartIndex;

		public readonly int NumBytes;

		public ulong BitMask => MakeBitMask(NumBits, StartBitIndex);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong MakeBitMask(int numBits, int startBitIndex)
		{
			return ~(ulong.MaxValue >> numBits << numBits) << startBitIndex;
		}

		public BitPositionValue64(int numBits, int startBitIndex, int startIndex = 0, int numBytes = 1)
			: this(MakeBitMask(numBits, startBitIndex), startIndex, numBytes)
		{
		}

		public BitPositionValue64(ulong bitMask, int startIndex = 0, int numBytes = 1)
		{
			int num = 0;
			int num2 = 0;
			if (numBytes <= 0)
			{
				throw new ArgumentException("BitPositionValue must have at least 1 byte of data");
			}
			if (numBytes > 8)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 1);
				defaultInterpolatedStringHandler.AppendLiteral("BitPositionValue numBytes must be less then ");
				defaultInterpolatedStringHandler.AppendFormatted(8);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			bool flag = false;
			for (int i = 0; i < 64; i++)
			{
				if (((bitMask >> i) & 1) == 0L)
				{
					if (flag)
					{
						break;
					}
					num2++;
					continue;
				}
				if (!flag)
				{
					flag = true;
					num2 = i;
				}
				num++;
			}
			NumBits = num;
			StartBitIndex = num2;
			StartIndex = startIndex;
			NumBytes = numBytes;
		}

		public ulong DecodeValue(ulong encodedValue)
		{
			ulong num = ~(ulong.MaxValue >> NumBits << NumBits) << StartBitIndex;
			return (encodedValue & num) >> StartBitIndex;
		}

		public ulong EncodeValue(ulong value, ulong mergeValue = 0uL)
		{
			ulong num = value << StartBitIndex;
			ulong num2 = ~(ulong.MaxValue >> NumBits << NumBits) << StartBitIndex;
			return (num2 & num) | (mergeValue & ~num2);
		}

		public ulong DecodeValueFromBuffer(byte[] dataBuffer)
		{
			if (dataBuffer == null || NumBytes <= 0)
			{
				return 0uL;
			}
			if (StartIndex < 0 || StartIndex + NumBytes > dataBuffer.Length)
			{
				throw new ArgumentException("DecodeValueFromBuffer can't extend past end of buffer");
			}
			ulong num = 0uL;
			for (int i = StartIndex; i < StartIndex + NumBytes; i++)
			{
				num <<= 8;
				num |= dataBuffer[i];
			}
			return DecodeValue(num);
		}

		public void EncodeValueToBuffer(ulong value, byte[] dataBuffer)
		{
			if (dataBuffer != null && NumBytes > 0)
			{
				if (StartIndex < 0 || StartIndex + NumBytes > dataBuffer.Length)
				{
					throw new ArgumentException("DecodeValueToBuffer can't extend past bounds of buffer");
				}
				ulong num = 0uL;
				for (int i = StartIndex; i < StartIndex + NumBytes; i++)
				{
					num <<= 8;
					num |= dataBuffer[i];
				}
				ulong num2 = EncodeValue(value, num);
				for (int num3 = StartIndex + NumBytes - 1; num3 >= StartIndex; num3--)
				{
					dataBuffer[num3] = (byte)(num2 & 0xFF);
					num2 >>= 8;
				}
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 4);
			defaultInterpolatedStringHandler.AppendLiteral("0b");
			defaultInterpolatedStringHandler.AppendFormatted(Convert.ToString((long)BitMask, 2));
			defaultInterpolatedStringHandler.AppendLiteral("(0x");
			defaultInterpolatedStringHandler.AppendFormatted(BitMask, "X");
			defaultInterpolatedStringHandler.AppendLiteral(")/");
			defaultInterpolatedStringHandler.AppendFormatted(StartIndex);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(NumBytes);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
