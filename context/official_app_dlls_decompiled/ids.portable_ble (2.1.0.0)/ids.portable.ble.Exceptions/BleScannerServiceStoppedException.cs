using System;

namespace ids.portable.ble.Exceptions
{
	public class BleScannerServiceStoppedException : BleServiceException
	{
		public BleScannerServiceStoppedException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
