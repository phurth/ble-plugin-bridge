namespace IDS.Portable.LogicalDevice
{
	public class SessionManagerNotAvailableException : LogicalDeviceSessionException
	{
		public SessionManagerNotAvailableException(string tag, ILogicalDevice logicalDevice)
			: base(tag, $"session manager isn't available for {logicalDevice}", verbose: false)
		{
		}
	}
}
