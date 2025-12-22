using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common.COBS
{
	public class CobsEncoder : CobsBase
	{
		private readonly byte[] _outputBuffer = new byte[382];

		public readonly bool PrependStartFrame;

		public CobsEncoder(bool prependStartFrame = true, bool useCrc = true, byte frameByte = 0, int numDataBits = 6)
			: base(useCrc, frameByte, numDataBits)
		{
			PrependStartFrame = prependStartFrame;
		}

		public IReadOnlyList<byte> Encode(IReadOnlyList<byte> source)
		{
			int num = 381;
			int num2 = 0;
			if (PrependStartFrame)
			{
				num--;
				_outputBuffer[num2++] = FrameCharacter;
			}
			if (source == null || source.Count <= 0)
			{
				return new ArraySegment<byte>(_outputBuffer, 0, num2);
			}
			if (UseCrc)
			{
				num--;
			}
			if (source.Count > num)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Source buffer is too big, must be <= ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				throw new ArgumentOutOfRangeException("source", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			Crc8 crc = default(Crc8);
			Crc8.Calculate(source);
			int num3 = 0;
			int count = source.Count;
			int num4 = (UseCrc ? (count + 1) : count);
			while (num3 < num4)
			{
				int num5 = num2;
				int num6 = 0;
				_outputBuffer[num2++] = byte.MaxValue;
				while (num3 < num4)
				{
					byte b;
					if (num3 < count)
					{
						b = source[num3];
						if (b == FrameCharacter)
						{
							break;
						}
						crc.Update(b);
					}
					else
					{
						b = crc.Value;
						if (b == FrameCharacter)
						{
							break;
						}
					}
					num3++;
					_outputBuffer[num2++] = b;
					num6++;
					if (num6 >= MaxDataBytes)
					{
						break;
					}
				}
				while (num3 < num4 && ((num3 < count) ? source[num3] : crc.Value) == FrameCharacter)
				{
					crc.Update(FrameCharacter);
					num3++;
					num6 += FrameByteCountLsb;
					if (num6 >= MaxCompressedFrameBytes)
					{
						break;
					}
				}
				_outputBuffer[num5] = (byte)num6;
			}
			_outputBuffer[num2++] = FrameCharacter;
			return new ArraySegment<byte>(_outputBuffer, 0, num2);
		}
	}
}
