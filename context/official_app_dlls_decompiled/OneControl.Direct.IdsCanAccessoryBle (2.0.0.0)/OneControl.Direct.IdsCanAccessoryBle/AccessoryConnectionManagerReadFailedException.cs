using System;
using System.Runtime.CompilerServices;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public class AccessoryConnectionManagerReadFailedException : Exception
	{
		public AccessoryConnectionManagerReadFailedException(ILogicalDevice logicalDevice, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Failed to read a characteristic on: ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
