using System;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlException : Exception
	{
		public EchoBrakeControlException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
