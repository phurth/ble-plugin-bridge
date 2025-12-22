using System.ComponentModel;

namespace OneControl.Devices
{
	[DefaultValue(LogicalDeviceLevelerOperationAutoType4.Unknown)]
	public enum LogicalDeviceLevelerOperationAutoType4
	{
		Unknown = -1,
		AutoLevel = 1,
		AutoHitch = 11,
		AutoRetractAllJacks = 12,
		AutoRetractFrontJacks = 13,
		AutoRetractRearJacks = 14,
		AutoHomeJacks = 15
	}
}
