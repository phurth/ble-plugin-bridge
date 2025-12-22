using System;
using System.Runtime.CompilerServices;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public class AccessoryConnectionManagerAccessoryOfflineException : Exception
	{
		public AccessoryConnectionManagerAccessoryOfflineException(ILogicalDevice logicalDevice, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Could not connect to accessory: ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
