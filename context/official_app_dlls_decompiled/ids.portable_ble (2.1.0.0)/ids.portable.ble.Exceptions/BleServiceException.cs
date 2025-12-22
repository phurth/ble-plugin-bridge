using System;

namespace ids.portable.ble.Exceptions
{
	public class BleServiceException : Exception
	{
		public BleServiceException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
