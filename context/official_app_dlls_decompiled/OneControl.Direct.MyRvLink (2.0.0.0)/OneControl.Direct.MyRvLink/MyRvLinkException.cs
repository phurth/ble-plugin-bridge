using System;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkException : Exception
	{
		public MyRvLinkException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
