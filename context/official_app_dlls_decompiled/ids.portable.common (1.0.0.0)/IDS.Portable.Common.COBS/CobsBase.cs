using System;

namespace IDS.Portable.Common.COBS
{
	public abstract class CobsBase
	{
		public const int MaxCobsSourceBufferSize = 255;

		public const int MaxOutputBufferSize = 382;

		public const byte DefaultFrameByte = 0;

		public const int DefaultDataBits = 6;

		public readonly bool UseCrc;

		public readonly byte FrameCharacter;

		public readonly int DataBitCount;

		protected readonly int FrameByteCountLsb;

		protected readonly int MaxDataBytes;

		protected readonly int MaxCompressedFrameBytes;

		protected CobsBase(bool useCrc, byte frameByte = 0, int numDataBits = 6)
		{
			UseCrc = useCrc;
			FrameCharacter = frameByte;
			if (numDataBits <= 1 || numDataBits > 8)
			{
				throw new ArgumentOutOfRangeException("numDataBits", "Number of data bits must be between 1 and 8");
			}
			DataBitCount = numDataBits;
			FrameByteCountLsb = 1 << DataBitCount;
			MaxDataBytes = FrameByteCountLsb - 1;
			MaxCompressedFrameBytes = 255 - MaxDataBytes;
		}
	}
}
