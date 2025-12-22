using System;
using System.Runtime.CompilerServices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceServiceNotStartedException : Exception
	{
		public MyRvLinkDeviceServiceNotStartedException(DirectConnectionMyRvLink myRvLink, string message, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 2);
			defaultInterpolatedStringHandler.AppendFormatted(myRvLink);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(message);
			defaultInterpolatedStringHandler.AppendLiteral(": Device Service Not Started");
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
