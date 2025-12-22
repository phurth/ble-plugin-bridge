namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceEx
	{
		void LogicalDeviceAttached(ILogicalDevice logicalDevice);

		void LogicalDeviceDetached(ILogicalDevice logicalDevice);
	}
}
