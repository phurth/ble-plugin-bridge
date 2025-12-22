using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightDimmableFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if (DeviceCategory.GetDeviceCategory(logicalDeviceId) != DeviceCategory.Light)
			{
				return null;
			}
			LogicalDeviceLightDimmableCapability dimmableCapability = new LogicalDeviceLightDimmableCapability(rawCapability);
			return new LogicalDeviceLightDimmable(logicalDeviceId, dimmableCapability, service);
		}
	}
}
