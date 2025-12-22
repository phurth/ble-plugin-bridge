namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceExInTransitLockout : ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		void LogicalDeviceInTransitLockoutChanged(ILogicalDevice logicalDevice);
	}
}
