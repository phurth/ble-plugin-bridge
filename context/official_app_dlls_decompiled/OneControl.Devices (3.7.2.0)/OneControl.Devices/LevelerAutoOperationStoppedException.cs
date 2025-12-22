using System;

namespace OneControl.Devices
{
	public class LevelerAutoOperationStoppedException : LevelerException
	{
		public LevelerAutoOperationStoppedException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public LevelerAutoOperationStoppedException(Exception? innerException = null)
			: this("Auto Operation Stopped Before Completing", innerException)
		{
		}
	}
}
