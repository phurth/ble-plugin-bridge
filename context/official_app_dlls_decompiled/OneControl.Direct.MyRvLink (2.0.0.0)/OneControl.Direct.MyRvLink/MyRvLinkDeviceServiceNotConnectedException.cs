using System;
using System.Runtime.CompilerServices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceServiceNotConnectedException : Exception
	{
		public MyRvLinkDeviceServiceNotConnectedException(DirectConnectionMyRvLink myRvLink, string message, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 2);
			defaultInterpolatedStringHandler.AppendFormatted(myRvLink);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(message);
			defaultInterpolatedStringHandler.AppendLiteral(": Device Service Not Connected");
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
