using System;

namespace ids.portable.ble.Exceptions
{
	public class BlePermissionException : BleServiceException
	{
		public BlePermissionException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
