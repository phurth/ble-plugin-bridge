using System.ComponentModel;

namespace OneControl.Devices
{
	[DefaultValue(LogicalDeviceLevelerOperationManualType4.Unknown)]
	public enum LogicalDeviceLevelerOperationManualType4
	{
		Unknown = -1,
		JackMovementManual = 2,
		JackMovementManualConsole = 3,
		JackMovementZero = 4,
		JackMovementFaultManual = 8,
		JackMovementFaultManualConsole = 9
	}
}
