namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceExAlertChanged : ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		void LogicalDeviceAlertChanged(ILogicalDevice logicalDevice, ILogicalDeviceAlert fromAlert, ILogicalDeviceAlert toAlert);
	}
}
