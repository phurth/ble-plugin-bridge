using System;
using System.Runtime.CompilerServices;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlNullReferenceException : EchoBrakeControlException
	{
		public EchoBrakeControlNullReferenceException(string logTag, string functionName, string variableName, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 3);
			defaultInterpolatedStringHandler.AppendFormatted(logTag);
			defaultInterpolatedStringHandler.AppendLiteral(": Null Reference Error - ");
			defaultInterpolatedStringHandler.AppendFormatted(variableName);
			defaultInterpolatedStringHandler.AppendLiteral(" is null in the function ");
			defaultInterpolatedStringHandler.AppendFormatted(functionName);
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
		}
	}
}
