namespace IDS.Portable.LogicalDevice
{
	public class ActivateSessionRemoteActiveException : LogicalDeviceSessionException
	{
		public ActivateSessionRemoteActiveException(string tag)
			: base(tag, "Remote mode is active so we can't active a CAN bus session")
		{
		}
	}
}
