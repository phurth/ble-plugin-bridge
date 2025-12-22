using System;
using System.Collections.Generic;

namespace IDS.Portable.LogicalDevice
{
	public interface IDeviceDataPacketMutableExtended : IDeviceDataPacketMutable, IDeviceDataPacket
	{
		byte ExtendedByte { get; }

		DateTime? LastUpdatedTimestamp { get; }

		bool Update(IReadOnlyDictionary<byte, byte[]> inData, DateTime? timeUpdated = null);

		bool Update(byte[] inData, int length, byte extendedByte, DateTime? timeUpdated = null);

		bool Update(IReadOnlyList<byte> inData, uint length, byte extendedByte, DateTime? timeUpdated = null);
	}
}
