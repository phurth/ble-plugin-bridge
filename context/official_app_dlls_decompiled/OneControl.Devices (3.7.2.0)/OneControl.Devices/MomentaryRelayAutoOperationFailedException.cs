using System;

namespace OneControl.Devices
{
	public class MomentaryRelayAutoOperationFailedException : MomentaryRelayException
	{
		public MomentaryRelayAutoOperationFailedException(Exception? innerException = null)
			: base("Momentary Relay Auto Operation failed", innerException)
		{
		}
	}
}
