namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceExOnline : ILogicalDeviceEx
	{
		void LogicalDeviceOnlineChanged(ILogicalDevice logicalDevice);
	}
}
