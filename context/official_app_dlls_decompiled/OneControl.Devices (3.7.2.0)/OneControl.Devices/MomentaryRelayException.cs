using System;

namespace OneControl.Devices
{
	public class MomentaryRelayException : Exception
	{
		public MomentaryRelayException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
