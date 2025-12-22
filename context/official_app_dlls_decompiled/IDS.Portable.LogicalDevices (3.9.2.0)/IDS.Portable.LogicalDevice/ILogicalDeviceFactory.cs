namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceFactory
	{
		string UID { get; }

		ILogicalDevice? MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability);
	}
}
