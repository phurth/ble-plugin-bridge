using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	internal class LogicalDeviceLeveler5TouchpadFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 57)
			{
				return null;
			}
			return new LogicalDeviceLeveler5Touchpad(logicalDeviceId, service);
		}
	}
}
