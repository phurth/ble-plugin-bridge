using System;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common
{
	public struct BitPositionValue
	{
		public readonly int NumBits;

		public readonly int StartBitIndex;

		public readonly int StartIndex;

		public readonly int NumBytes;

		public uint BitMask => MakeBitMask(NumBits, StartBitIndex);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint MakeBitMask(int numBits, int startBitIndex)
		{
			return ~(uint.MaxValue >> numBits << numBits) << startBitIndex;
		}

		public BitPositionValue(int numBits, int startBitIndex, int startIndex = 0, int numBytes = 1)
			: this(MakeBitMask(numBits, startBitIndex), startIndex, numBytes)
		{
		}

		public BitPositionValue(uint bitMask, int startIndex = 0, int numBytes = 1)
		{
			int num = 0;
			int num2 = 0;
			if (numBytes <= 0)
			{
				throw new ArgumentException("BitPositionValue must have at least 1 byte of data");
			}
			if (numBytes > 4)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 1);
				defaultInterpolatedStringHandler.AppendLiteral("BitPositionValue numBytes must be less then ");
				defaultInterpolatedStringHandler.AppendFormatted(4);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			bool flag = false;
			for (int i = 0; i < 32; i++)
			{
				if (((bitMask >> i) & 1) == 0)
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

		public uint DecodeValue(uint encodedValue)
		{
			uint num = ~(uint.MaxValue >> NumBits << NumBits) << StartBitIndex;
			return (encodedValue & num) >> StartBitIndex;
		}

		public uint EncodeValue(uint value, uint mergeValue = 0u)
		{
			uint num = value << StartBitIndex;
			uint num2 = ~(uint.MaxValue >> NumBits << NumBits) << StartBitIndex;
			return (num2 & num) | (mergeValue & ~num2);
		}

		public uint DecodeValueFromBuffer(byte[] dataBuffer)
		{
			return DecodeValueFromBuffer(dataBuffer, StartIndex, NumBytes);
		}

		public uint DecodeValueFromBuffer(byte[] dataBuffer, int startIndex, int numBytes)
		{
			if (dataBuffer == null || NumBytes <= 0)
			{
				return 0u;
			}
			if (startIndex < 0 || startIndex + numBytes > dataBuffer.Length)
			{
				throw new ArgumentException("DecodeValueFromBuffer can't extend past end of buffer");
			}
			uint num = 0u;
			for (int i = startIndex; i < startIndex + numBytes; i++)
			{
				num <<= 8;
				num |= dataBuffer[i];
			}
			return DecodeValue(num);
		}

		public void EncodeValueToBuffer(uint value, byte[] dataBuffer)
		{
			if (dataBuffer != null && NumBytes > 0)
			{
				if (StartIndex < 0 || StartIndex + NumBytes > dataBuffer.Length)
				{
					throw new ArgumentException("DecodeValueToBuffer can't extend past bounds of buffer");
				}
				uint num = 0u;
				for (int i = StartIndex; i < StartIndex + NumBytes; i++)
				{
					num <<= 8;
					num |= dataBuffer[i];
				}
				uint num2 = EncodeValue(value, num);
				for (int num3 = StartIndex + NumBytes - 1; num3 >= StartIndex; num3--)
				{
					dataBuffer[num3] = (byte)(num2 & 0xFFu);
					num2 >>= 8;
				}
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 4);
			defaultInterpolatedStringHandler.AppendLiteral("0b");
			defaultInterpolatedStringHandler.AppendFormatted(Convert.ToString(BitMask, 2));
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
