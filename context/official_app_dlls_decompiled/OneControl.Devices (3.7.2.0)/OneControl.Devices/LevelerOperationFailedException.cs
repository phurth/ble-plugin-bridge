using System;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Leveler.Type5;

namespace OneControl.Devices
{
	public class LevelerOperationFailedException : LevelerException
	{
		public CommandResult Result { get; }

		public LevelerFaultType5 Fault { get; }

		public LevelerOperationFailedException(CommandResult result, LevelerFaultType5 fault, Exception? innerException = null)
			: base($"Operation Failed {result} with {fault.ToDebugString()}", innerException)
		{
			Result = result;
			Fault = fault;
		}
	}
}
