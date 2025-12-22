using System;

namespace ids.portable.ble.Exceptions
{
	public class BleManagerException : Exception
	{
		public BleManagerException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
