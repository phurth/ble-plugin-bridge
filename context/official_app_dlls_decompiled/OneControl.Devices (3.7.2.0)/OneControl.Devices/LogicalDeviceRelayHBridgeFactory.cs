using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if (DeviceCategory.GetDeviceCategory(logicalDeviceId).Function != DeviceCategory.AppFunction.RelayHBridge)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType == 6)
			{
				LogicalDeviceRelayCapabilityType1 capability = new LogicalDeviceRelayCapabilityType1(rawCapability);
				return new LogicalDeviceRelayHBridgeMomentaryType1(logicalDeviceId, capability, service);
			}
			if ((byte)logicalDeviceId.DeviceType == 33)
			{
				LogicalDeviceRelayHBridgeCapabilityType2 capability2 = new LogicalDeviceRelayHBridgeCapabilityType2(rawCapability);
				return new LogicalDeviceRelayHBridgeMomentaryType2(logicalDeviceId, capability2, service);
			}
			return null;
		}
	}
}
