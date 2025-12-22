using System;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDecoderException : Exception
	{
		public MyRvLinkDecoderException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
