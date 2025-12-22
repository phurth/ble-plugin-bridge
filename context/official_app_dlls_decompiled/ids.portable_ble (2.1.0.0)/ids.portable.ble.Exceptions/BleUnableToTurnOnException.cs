using System;

namespace ids.portable.ble.Exceptions
{
	public class BleUnableToTurnOnException : BleServiceException
	{
		public BleUnableToTurnOnException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
