using System.Collections.Generic;

namespace IDS.Portable.LogicalDevice
{
	public interface IDeviceDataPacketMutable : IDeviceDataPacket
	{
		byte[] Data { get; }

		int Update(IReadOnlyList<byte> inData, int length, bool forceUpdate = false);

		int CopyData(byte[] destinationBuff, int destinationOffset, int maxSize);
	}
}
