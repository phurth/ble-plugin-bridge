using System;
using System.Collections.Generic;

namespace IDS.Portable.LogicalDevice
{
	public interface IDeviceCommandPacket : IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		int CommandResponseTimeMs { get; }

		byte CommandByte { get; }

		IReadOnlyList<byte> RawData { get; }
	}
}
