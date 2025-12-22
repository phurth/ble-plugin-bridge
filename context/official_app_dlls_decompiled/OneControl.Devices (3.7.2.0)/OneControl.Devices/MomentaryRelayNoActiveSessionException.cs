using System;

namespace OneControl.Devices
{
	public class MomentaryRelayNoActiveSessionException : MomentaryRelayException
	{
		public MomentaryRelayNoActiveSessionException(Exception? innerException = null)
			: base("Active Session is required", innerException)
		{
		}
	}
}
