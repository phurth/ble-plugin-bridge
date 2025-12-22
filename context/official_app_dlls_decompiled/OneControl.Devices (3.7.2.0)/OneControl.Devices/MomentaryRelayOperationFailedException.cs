using System;

namespace OneControl.Devices
{
	public class MomentaryRelayOperationFailedException : MomentaryRelayException
	{
		public MomentaryRelayOperationFailedException(Exception? innerException = null)
			: base("Momentary Relay Operation failed", innerException)
		{
		}
	}
}
