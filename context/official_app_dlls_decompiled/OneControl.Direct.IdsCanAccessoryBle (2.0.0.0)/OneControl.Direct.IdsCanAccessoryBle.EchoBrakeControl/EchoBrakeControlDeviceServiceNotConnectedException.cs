using System;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlDeviceServiceNotConnectedException : EchoBrakeControlException
	{
		public EchoBrakeControlDeviceServiceNotConnectedException(string logTag, Exception? innerException = null)
			: base(logTag + ": Device Service Not Connected", innerException)
		{
		}
	}
}
