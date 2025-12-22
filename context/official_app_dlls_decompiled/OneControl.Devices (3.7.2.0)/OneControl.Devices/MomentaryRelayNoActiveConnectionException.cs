using System;

namespace OneControl.Devices
{
	public class MomentaryRelayNoActiveConnectionException : MomentaryRelayException
	{
		public MomentaryRelayNoActiveConnectionException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public MomentaryRelayNoActiveConnectionException(Exception? innerException = null)
			: base("No Active Connection (Momentary Relay offline)", innerException)
		{
		}
	}
}
