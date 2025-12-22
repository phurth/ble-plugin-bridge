using System.ComponentModel;

namespace OneControl.Devices
{
	[DefaultValue(LogicalDeviceLevelerScreenType4.Unknown)]
	public enum LogicalDeviceLevelerScreenType4
	{
		Home = 0,
		[LogicalDeviceLevelerOperationAutoType4]
		AutoLevel = 1,
		[LogicalDeviceLevelerOperationManualType4(false, false)]
		JackMovementManual = 2,
		[LogicalDeviceLevelerOperationManualType4(true, false)]
		JackMovementManualConsole = 3,
		[LogicalDeviceLevelerOperationManualType4(false, false)]
		JackMovementZero = 4,
		PromptInfo = 5,
		PromptYesNo = 6,
		PromptFault = 7,
		[LogicalDeviceLevelerOperationManualType4(false, true)]
		JackMovementFaultManual = 8,
		[LogicalDeviceLevelerOperationManualType4(true, true)]
		JackMovementFaultManualConsole = 9,
		[LogicalDeviceLevelerOperationManualType4(true, false)]
		AirSuspensionControlManual = 10,
		[LogicalDeviceLevelerOperationAutoType4]
		AutoHitch = 11,
		[LogicalDeviceLevelerOperationAutoType4]
		AutoRetractAllJacks = 12,
		[LogicalDeviceLevelerOperationAutoType4]
		AutoRetractFrontJacks = 13,
		[LogicalDeviceLevelerOperationAutoType4]
		AutoRetractRearJacks = 14,
		[LogicalDeviceLevelerOperationAutoType4]
		AutoHomeJacks = 15,
		PromptAirbagTimeSelect = 16,
		Unknown = -1
	}
}
