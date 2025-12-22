namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceExCapability : ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		void LogicalDeviceCapabilityChanged(ILogicalDevice logicalDevice);
	}
}
