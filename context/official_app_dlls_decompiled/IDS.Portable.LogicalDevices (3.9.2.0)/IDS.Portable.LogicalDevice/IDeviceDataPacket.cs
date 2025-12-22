namespace IDS.Portable.LogicalDevice
{
	public interface IDeviceDataPacket
	{
		bool HasData { get; }

		uint MinSize { get; }

		uint MaxSize { get; }

		uint Size { get; }

		bool GetBit(BasicBitMask bit);

		byte[] CopyCurrentData();
	}
}
