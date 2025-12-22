using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceHourMeterFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 12)
			{
				return null;
			}
			return new LogicalDeviceHourMeter(logicalDeviceId, service);
		}
	}
}
