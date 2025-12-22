using System;

namespace OneControl.Devices
{
	public class LevelerNoActiveConnectionException : LevelerException
	{
		public LevelerNoActiveConnectionException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public LevelerNoActiveConnectionException(Exception? innerException = null)
			: base("No Active Connection (leveler offline)", innerException)
		{
		}
	}
}
