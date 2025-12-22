using System;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Leveler.Type5;

namespace OneControl.Devices
{
	public class LevelerAutoOperationFailedWithoutRetryException : LevelerOperationFailedException
	{
		public LevelerAutoOperationFailedWithoutRetryException(CommandResult result, LevelerFaultType5 fault, Exception? innerException = null)
			: base(result, fault, innerException)
		{
		}
	}
}
