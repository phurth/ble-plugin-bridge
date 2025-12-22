using System;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public class AccessoryConnectionManagerUnsafeToRebootException : Exception
	{
		public AccessoryConnectionManagerUnsafeToRebootException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
