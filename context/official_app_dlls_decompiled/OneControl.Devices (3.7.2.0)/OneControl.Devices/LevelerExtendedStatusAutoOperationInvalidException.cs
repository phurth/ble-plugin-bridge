using System;

namespace OneControl.Devices
{
	public class LevelerExtendedStatusAutoOperationInvalidException : LevelerException
	{
		public LevelerExtendedStatusAutoOperationInvalidException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
