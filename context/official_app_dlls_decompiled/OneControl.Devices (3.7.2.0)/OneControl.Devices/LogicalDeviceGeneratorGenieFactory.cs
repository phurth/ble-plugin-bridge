using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceGeneratorGenieFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 24)
			{
				return null;
			}
			return new LogicalDeviceGeneratorGenie(logicalDeviceId, new LogicalDeviceGeneratorGenieCapability(rawCapability), service);
		}
	}
}
