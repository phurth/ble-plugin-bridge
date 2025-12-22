using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceUnknownRemoteFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			return new LogicalDeviceUnknownRemote(service, logicalDeviceId, rawCapability);
		}
	}
}
