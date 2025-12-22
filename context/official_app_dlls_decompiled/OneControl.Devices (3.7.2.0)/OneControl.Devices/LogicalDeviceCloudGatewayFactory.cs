using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceCloudGatewayFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 29)
			{
				return null;
			}
			return new LogicalDeviceCloudGateway(logicalDeviceId, service);
		}
	}
}
