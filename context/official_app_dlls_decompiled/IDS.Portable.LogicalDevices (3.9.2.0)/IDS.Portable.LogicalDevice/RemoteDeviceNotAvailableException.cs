using System;

namespace IDS.Portable.LogicalDevice
{
	public class RemoteDeviceNotAvailableException : Exception
	{
		public RemoteDeviceNotAvailableException(Exception? innerException = null)
			: base("Remote Device Not Available", innerException)
		{
		}
	}
}
