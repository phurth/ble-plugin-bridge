using System;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public class AccessoryConnectionManagerReadDataException : Exception
	{
		public AccessoryConnectionManagerReadDataException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
