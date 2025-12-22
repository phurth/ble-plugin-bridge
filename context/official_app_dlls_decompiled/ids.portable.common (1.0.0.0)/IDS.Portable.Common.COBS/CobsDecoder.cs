using System;
using System.Collections.Generic;

namespace IDS.Portable.Common.COBS
{
	public class CobsDecoder : CobsBase
	{
		private const string LogTag = "CobsDecoder";

		private int _codeByte;

		private int _destinationNextByteIndex;

		private readonly byte[] _outputBuffer = new byte[382];

		private readonly int _minPayloadSize;

		public CobsDecoder(bool useCrc = true, byte frameByte = 0, int numDataBits = 6)
			: base(useCrc, frameByte, numDataBits)
		{
			_minPayloadSize = (useCrc ? 1 : 0);
		}

		public IReadOnlyList<byte>? TryDecodeByte(byte b)
		{
			try
			{
				return DecodeByte(b);
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("CobsDecoder", "Error decoding packet {0}", ex.Message);
				return null;
			}
		}

		public IReadOnlyList<byte>? DecodeByte(byte b)
		{
			if (b == FrameCharacter)
			{
				try
				{
					if (_codeByte != 0)
					{
						return null;
					}
					if (_destinationNextByteIndex <= _minPayloadSize)
					{
						return null;
					}
					if (UseCrc)
					{
						byte b2 = _outputBuffer[_destinationNextByteIndex - 1];
						_destinationNextByteIndex--;
						byte b3 = Crc8.Calculate(_outputBuffer, _destinationNextByteIndex);
						if (b3 != b2)
						{
							throw new CobsCrcMismatchException(b3, b2, new ArraySegment<byte>(_outputBuffer, 0, _destinationNextByteIndex));
						}
					}
					return new ArraySegment<byte>(_outputBuffer, 0, _destinationNextByteIndex);
				}
				finally
				{
					_codeByte = 0;
					_destinationNextByteIndex = 0;
				}
			}
			if (_codeByte <= 0)
			{
				_codeByte = b & 0xFF;
			}
			else
			{
				_codeByte--;
				_outputBuffer[_destinationNextByteIndex++] = b;
			}
			if ((_codeByte & MaxDataBytes) == 0)
			{
				while (_codeByte > 0)
				{
					_outputBuffer[_destinationNextByteIndex++] = FrameCharacter;
					_codeByte -= FrameByteCountLsb;
				}
			}
			return null;
		}
	}
}
