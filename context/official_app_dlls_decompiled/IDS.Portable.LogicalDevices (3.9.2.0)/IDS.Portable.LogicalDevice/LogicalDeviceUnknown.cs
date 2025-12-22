namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceUnknown : LogicalDevice<LogicalDeviceStatusPacketMutable, ILogicalDeviceCapability>
	{
		public LogicalDeviceUnknown(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
			: base(logicalDeviceId, new LogicalDeviceStatusPacketMutable(0u), (ILogicalDeviceCapability)new LogicalDeviceCapability(rawCapability), service, isFunctionClassChangeable: false)
		{
		}
	}
}
