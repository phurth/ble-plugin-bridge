using System;

namespace OneControl.Direct.MyRvLinkBle
{
	public class MyRvLinkBleServiceException : Exception
	{
		public MyRvLinkBleServiceException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
