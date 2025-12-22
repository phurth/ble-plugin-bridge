using System;
using System.Runtime.CompilerServices;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceNotFoundException : Exception
	{
		public MyRvLinkDeviceNotFoundException(DirectConnectionMyRvLink myRvLink, ILogicalDevice logicalDevice, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 2);
			defaultInterpolatedStringHandler.AppendFormatted(myRvLink);
			defaultInterpolatedStringHandler.AppendLiteral(": Unable to find device for ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
