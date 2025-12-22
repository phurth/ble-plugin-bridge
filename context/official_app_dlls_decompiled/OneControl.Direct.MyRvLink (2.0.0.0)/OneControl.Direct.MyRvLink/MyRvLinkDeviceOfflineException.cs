using System;
using System.Runtime.CompilerServices;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceOfflineException : Exception
	{
		public MyRvLinkDeviceOfflineException(DirectConnectionMyRvLink myRvLink, ILogicalDevice logicalDevice, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
			defaultInterpolatedStringHandler.AppendFormatted(myRvLink);
			defaultInterpolatedStringHandler.AppendLiteral(": Device offline for ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
