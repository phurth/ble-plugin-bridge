using System;

namespace OneControl.Devices
{
	public class LevelerException : Exception
	{
		public LevelerException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
