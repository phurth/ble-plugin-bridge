using System;

namespace OneControl.Devices
{
	public class LevelerSessionEndedException : LevelerException
	{
		public LevelerSessionEndedException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public LevelerSessionEndedException(Exception? innerException = null)
			: base("Session ended while trying to perform an auto operation", innerException)
		{
		}
	}
}
