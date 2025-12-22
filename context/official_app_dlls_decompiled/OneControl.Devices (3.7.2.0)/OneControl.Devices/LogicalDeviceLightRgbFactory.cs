using IDS.Portable.LogicalDevice;
using OneControl.Devices.LightRgb;

namespace OneControl.Devices
{
	public class LogicalDeviceLightRgbFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 13)
			{
				return null;
			}
			LogicalDeviceLightRgbCapability rgbCapability = new LogicalDeviceLightRgbCapability(rawCapability);
			return new LogicalDeviceLightRgb(logicalDeviceId, rgbCapability, service);
		}
	}
}
