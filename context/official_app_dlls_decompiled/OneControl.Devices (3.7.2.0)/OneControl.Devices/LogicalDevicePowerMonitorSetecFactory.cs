using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDevicePowerMonitorSetecFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 28)
			{
				return null;
			}
			return new LogicalDevicePowerMonitorSetec(logicalDeviceId, service);
		}
	}
}
