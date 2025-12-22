using System;

namespace IDS.Portable.LogicalDevice
{
	[Flags]
	public enum CommandSendOption
	{
		None = 0,
		CancelCurrentCommand = 1,
		AutoClearLockoutDisabled = 2
	}
}
