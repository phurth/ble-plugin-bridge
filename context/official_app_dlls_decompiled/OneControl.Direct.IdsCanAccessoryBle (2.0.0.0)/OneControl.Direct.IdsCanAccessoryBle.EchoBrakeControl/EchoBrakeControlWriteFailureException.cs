using System;
using System.Runtime.CompilerServices;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlWriteFailureException : EchoBrakeControlException
	{
		public EchoBrakeControlWriteFailureException(string logTag, string characteristic, string functionName, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 3);
			defaultInterpolatedStringHandler.AppendFormatted(logTag);
			defaultInterpolatedStringHandler.AppendLiteral(": Failed to write to characteristic: ");
			defaultInterpolatedStringHandler.AppendFormatted(characteristic);
			defaultInterpolatedStringHandler.AppendLiteral(" in function ");
			defaultInterpolatedStringHandler.AppendFormatted(functionName);
			defaultInterpolatedStringHandler.AppendLiteral(".");
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
