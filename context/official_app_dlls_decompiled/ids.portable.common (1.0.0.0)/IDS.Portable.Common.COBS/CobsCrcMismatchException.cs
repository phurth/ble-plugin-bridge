using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.Common.COBS
{
	public class CobsCrcMismatchException : Exception
	{
		public CobsCrcMismatchException(byte computedCrc, byte payloadCrc, IReadOnlyList<byte> decodedData)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 3);
			defaultInterpolatedStringHandler.AppendLiteral("CRC 0x");
			defaultInterpolatedStringHandler.AppendFormatted(computedCrc, "X");
			defaultInterpolatedStringHandler.AppendLiteral(" != 0x");
			defaultInterpolatedStringHandler.AppendFormatted(payloadCrc, "X");
			defaultInterpolatedStringHandler.AppendLiteral(" for ");
			defaultInterpolatedStringHandler.AppendFormatted(decodedData.DebugDump(0, decodedData.Count));
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}
}
