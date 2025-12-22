using System;
using ids.portable.ble.Exceptions;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public class IdsCanAccessoryException : BleServiceException
	{
		public IdsCanAccessoryException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
