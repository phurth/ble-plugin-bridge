using System;

namespace OneControl.Devices
{
	public class LevelerAutoOperationInProgressException : LevelerException
	{
		public LevelerAutoOperationInProgressException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
