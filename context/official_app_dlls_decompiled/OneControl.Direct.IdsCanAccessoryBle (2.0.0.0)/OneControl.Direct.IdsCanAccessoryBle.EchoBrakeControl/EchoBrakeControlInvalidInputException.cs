using System;
using System.Runtime.CompilerServices;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlInvalidInputException : EchoBrakeControlException
	{
		public EchoBrakeControlInvalidInputException(string logTag, string reason, string functionName, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 3);
			defaultInterpolatedStringHandler.AppendFormatted(logTag);
			defaultInterpolatedStringHandler.AppendLiteral(": Value sent into ");
			defaultInterpolatedStringHandler.AppendFormatted(functionName);
			defaultInterpolatedStringHandler.AppendLiteral(" is in valid. Reason: ");
			defaultInterpolatedStringHandler.AppendFormatted(reason);
			defaultInterpolatedStringHandler.AppendLiteral(".");
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
