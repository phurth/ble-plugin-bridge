using System;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public class AccessoryConnectionManagerException : Exception
	{
		public AccessoryConnectionManagerException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
