using System;

namespace OneControl.Devices
{
	public class MomentaryRelayOperationInProgressException : MomentaryRelayException
	{
		public MomentaryRelayOperationInProgressException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
