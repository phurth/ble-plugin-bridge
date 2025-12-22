namespace IDS.Portable.LogicalDevice
{
	public class DefaultLogicalDeviceFactory : ILogicalDeviceFactory
	{
		public string UID => GetType().Name;

		public virtual ILogicalDevice? MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			return new LogicalDeviceUnknown(service, logicalDeviceId, rawCapability);
		}
	}
}
