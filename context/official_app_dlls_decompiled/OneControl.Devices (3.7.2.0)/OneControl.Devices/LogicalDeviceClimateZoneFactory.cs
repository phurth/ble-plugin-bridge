using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceClimateZoneFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 16)
			{
				return null;
			}
			return new LogicalDeviceClimateZone(logicalDeviceId, new LogicalDeviceClimateZoneCapability(rawCapability), service);
		}
	}
}
