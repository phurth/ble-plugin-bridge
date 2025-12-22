using System;

namespace IDS.Portable.LogicalDevice
{
	[Flags]
	public enum FunctionNameDetailUse : uint
	{
		Unknown = 0u,
		Awning = 1u,
		BatteryMonitor = 2u
	}
}
