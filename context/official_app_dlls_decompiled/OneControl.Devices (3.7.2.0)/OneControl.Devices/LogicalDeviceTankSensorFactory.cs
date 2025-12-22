using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceTankSensorFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 10)
			{
				return null;
			}
			return new LogicalDeviceTankSensor(logicalDeviceId, new LogicalDeviceTankSensorCapability(rawCapability), service);
		}
	}
}
