using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDevicePowerMonitorDcFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 27)
			{
				return null;
			}
			return new LogicalDevicePowerMonitorDc(logicalDeviceId, new LogicalDevicePowerMonitorDcCapability(rawCapability), service);
		}
	}
}
