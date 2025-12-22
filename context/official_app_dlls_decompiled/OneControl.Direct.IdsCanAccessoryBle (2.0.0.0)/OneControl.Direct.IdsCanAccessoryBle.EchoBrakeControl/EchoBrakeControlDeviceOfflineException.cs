using System;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlDeviceOfflineException : EchoBrakeControlException
	{
		public EchoBrakeControlDeviceOfflineException(Exception? innerException = null)
			: this("Echo Brake Control Offline", innerException)
		{
		}

		public EchoBrakeControlDeviceOfflineException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
