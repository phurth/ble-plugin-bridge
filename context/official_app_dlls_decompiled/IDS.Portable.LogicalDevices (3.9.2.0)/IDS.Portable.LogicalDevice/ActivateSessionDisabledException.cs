namespace IDS.Portable.LogicalDevice
{
	public class ActivateSessionDisabledException : LogicalDeviceSessionException
	{
		public ActivateSessionDisabledException(string tag)
			: base(tag, "Unable to activate session because sessions have been disabled")
		{
		}
	}
}
