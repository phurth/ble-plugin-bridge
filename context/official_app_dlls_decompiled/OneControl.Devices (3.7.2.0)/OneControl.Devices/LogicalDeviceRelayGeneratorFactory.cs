using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	internal class LogicalDeviceRelayGeneratorFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if (DeviceCategory.GetDeviceCategory(logicalDeviceId) != DeviceCategory.Generator)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType == 6)
			{
				LogicalDeviceRelayCapabilityType1 capability = new LogicalDeviceRelayCapabilityType1(rawCapability);
				return new LogicalDeviceRelayGeneratorType1(logicalDeviceId, capability, service);
			}
			if ((byte)logicalDeviceId.DeviceType == 33)
			{
				LogicalDeviceRelayCapabilityType2 capability2 = new LogicalDeviceRelayCapabilityType2(rawCapability);
				return new LogicalDeviceRelayGeneratorType2(logicalDeviceId, capability2, service);
			}
			return null;
		}
	}
}
