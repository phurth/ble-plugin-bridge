using System;

namespace OneControl.Devices
{
	public class MomentaryRelayInLockoutException : MomentaryRelayException
	{
		public MomentaryRelayInLockoutException(Exception? innerException = null)
			: base("No Active Connection (Momentary Relay offline)", innerException)
		{
		}
	}
}
