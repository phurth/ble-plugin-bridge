using System;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlDeviceNotFoundException : EchoBrakeControlException
	{
		public EchoBrakeControlDeviceNotFoundException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
