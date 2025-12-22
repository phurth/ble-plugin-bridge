using System;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlReadException : EchoBrakeControlException
	{
		public EchoBrakeControlReadException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
