namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceExSnapshot : ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		void LogicalDeviceSnapshotLoaded(ILogicalDevice logicalDevice, LogicalDeviceSnapshot snapshot);
	}
}
