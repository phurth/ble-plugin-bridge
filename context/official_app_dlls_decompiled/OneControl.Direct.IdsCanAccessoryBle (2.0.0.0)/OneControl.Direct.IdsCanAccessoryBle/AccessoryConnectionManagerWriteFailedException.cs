using System;
using System.Runtime.CompilerServices;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public class AccessoryConnectionManagerWriteFailedException : Exception
	{
		public AccessoryConnectionManagerWriteFailedException(ILogicalDevice logicalDevice, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Failed to write a characteristic on: ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
