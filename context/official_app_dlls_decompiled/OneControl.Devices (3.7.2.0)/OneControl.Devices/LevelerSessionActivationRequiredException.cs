using System;

namespace OneControl.Devices
{
	public class LevelerSessionActivationRequiredException : LevelerException
	{
		public LevelerSessionActivationRequiredException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public LevelerSessionActivationRequiredException(Exception? innerException = null)
			: this("Session Activation is Required", innerException)
		{
		}
	}
}
