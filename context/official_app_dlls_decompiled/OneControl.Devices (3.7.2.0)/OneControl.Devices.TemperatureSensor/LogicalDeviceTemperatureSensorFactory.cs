using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.TemperatureSensor
{
	public class LogicalDeviceTemperatureSensorFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 25)
			{
				return null;
			}
			return new LogicalDeviceTemperatureSensor(logicalDeviceId, new LogicalDeviceTemperatureSensorCapability(rawCapability), service);
		}
	}
}
