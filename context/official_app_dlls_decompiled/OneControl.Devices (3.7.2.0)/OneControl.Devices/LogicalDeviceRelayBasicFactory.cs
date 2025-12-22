using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType == 3)
			{
				LogicalDeviceRelayCapabilityType1 capability = new LogicalDeviceRelayCapabilityType1(rawCapability);
				return new LogicalDeviceRelayBasicLatchingType1(logicalDeviceId, capability, service);
			}
			if ((byte)logicalDeviceId.DeviceType == 30)
			{
				LogicalDeviceRelayCapabilityType2 capability2 = new LogicalDeviceRelayCapabilityType2(rawCapability);
				return new LogicalDeviceRelayBasicLatchingType2(logicalDeviceId, capability2, service);
			}
			return null;
		}
	}
}
