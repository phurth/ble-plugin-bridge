namespace IDS.Portable.LogicalDevice
{
	public class ActivateSessionDeviceOffline : LogicalDeviceSessionException
	{
		public ActivateSessionDeviceOffline(string tag, ILogicalDevice logicalDevice)
			: base(tag, $"Unable to get session for offline device {logicalDevice}")
		{
		}
	}
}
