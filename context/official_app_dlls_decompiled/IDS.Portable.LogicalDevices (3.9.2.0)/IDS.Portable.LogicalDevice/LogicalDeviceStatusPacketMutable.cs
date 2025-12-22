namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceStatusPacketMutable : LogicalDeviceDataPacketMutableDoubleBuffer
	{
		public LogicalDeviceStatusPacketMutable(uint minSize)
			: base(minSize, 8u, 0)
		{
		}

		public LogicalDeviceStatusPacketMutable(uint minSize, uint maxSize, byte bufferFill = 0)
			: base(minSize, maxSize, bufferFill)
		{
		}

		public LogicalDeviceStatusPacketMutable()
			: base(1u, 8u, 0)
		{
		}
	}
}
