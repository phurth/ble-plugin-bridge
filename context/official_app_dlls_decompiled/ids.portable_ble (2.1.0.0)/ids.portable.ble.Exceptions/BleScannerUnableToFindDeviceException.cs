using System;

namespace ids.portable.ble.Exceptions
{
	public class BleScannerUnableToFindDeviceException : BleServiceException
	{
		public BleScannerUnableToFindDeviceException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
