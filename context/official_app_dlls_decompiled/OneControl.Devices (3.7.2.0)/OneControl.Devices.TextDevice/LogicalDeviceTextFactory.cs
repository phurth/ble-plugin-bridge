using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.TextDevice
{
	public class LogicalDeviceTextFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if (!(logicalDeviceId is LogicalDeviceTextId logicalDeviceId2))
			{
				return null;
			}
			return new LogicalDeviceText(logicalDeviceId2, service);
		}
	}
}
