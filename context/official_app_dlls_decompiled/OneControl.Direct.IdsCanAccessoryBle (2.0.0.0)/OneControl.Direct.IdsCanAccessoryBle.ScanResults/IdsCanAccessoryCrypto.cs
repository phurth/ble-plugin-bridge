using System;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Exceptions;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	internal readonly struct IdsCanAccessoryCrypto
	{
		private const string LogTag = "IdsCanAccessoryCrypto";

		private readonly ulong _accessoryMacAddress;

		private const int BitsPerByte = 8;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private uint Random32(uint key)
		{
			key *= 1103515245;
			key += 2663591993u;
			return key;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (byte Rand8, uint Key) Random8(uint key)
		{
			key = Random32(key);
			return ((byte)(key >> 24), key);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IdsCanAccessoryCrypto(MAC accessoryMacAddress)
		{
			_accessoryMacAddress = accessoryMacAddress;
		}

		public void DecryptPacket(ref byte[] buffer, int startOffset)
		{
			int num = buffer.Length;
			if (buffer.Length - startOffset < 5)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Not enough space in buffer with given startOffset ");
				defaultInterpolatedStringHandler.AppendFormatted(startOffset);
				throw new ArgumentOutOfRangeException("buffer", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			uint num2 = (uint)(buffer[--num] ^ (byte)_accessoryMacAddress);
			num2 |= (uint)((buffer[--num] ^ (byte)(_accessoryMacAddress >> 8)) << 8);
			num2 |= (uint)((buffer[--num] ^ (byte)(_accessoryMacAddress >> 16)) << 16);
			num2 |= (uint)((buffer[--num] ^ (byte)(_accessoryMacAddress >> 24)) << 24);
			byte b = buffer[--num];
			int count = num - startOffset;
			for (int i = startOffset; i < num; i++)
			{
				byte b2;
				(b2, num2) = Random8(num2);
				buffer[i] ^= b2;
			}
			byte b3 = Crc8.Calculate(buffer, count, startOffset);
			if (b != b3)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 3);
				defaultInterpolatedStringHandler.AppendLiteral("CRC Check Failed ");
				defaultInterpolatedStringHandler.AppendFormatted(b, "X");
				defaultInterpolatedStringHandler.AppendLiteral(" != ");
				defaultInterpolatedStringHandler.AppendFormatted(b3, "X");
				defaultInterpolatedStringHandler.AppendLiteral("\n Decoded Buffer:");
				defaultInterpolatedStringHandler.AppendFormatted(buffer.DebugDump());
				TaggedLog.Debug("IdsCanAccessoryCrypto", defaultInterpolatedStringHandler.ToStringAndClear());
				throw new AccessoryCryptoCrcCheckException(b, b3);
			}
		}
	}
}
