namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceExStatus : ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		void LogicalDeviceStatusChanged(ILogicalDevice logicalDevice);
	}
}
