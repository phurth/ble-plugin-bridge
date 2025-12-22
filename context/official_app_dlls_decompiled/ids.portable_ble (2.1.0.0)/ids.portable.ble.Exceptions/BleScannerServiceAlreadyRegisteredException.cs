using System;

namespace ids.portable.ble.Exceptions
{
	public class BleScannerServiceAlreadyRegisteredException : BleServiceException
	{
		public BleScannerServiceAlreadyRegisteredException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
