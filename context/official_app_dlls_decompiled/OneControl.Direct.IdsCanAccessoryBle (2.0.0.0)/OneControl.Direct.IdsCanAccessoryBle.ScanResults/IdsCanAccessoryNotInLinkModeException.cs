using System;
using System.Runtime.CompilerServices;
using ids.portable.ble.Exceptions;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public class IdsCanAccessoryNotInLinkModeException : BleServiceException
	{
		public IdsCanAccessoryNotInLinkModeException(Guid deviceId, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Link mode is required for operation on device ");
			defaultInterpolatedStringHandler.AppendFormatted(deviceId);
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
