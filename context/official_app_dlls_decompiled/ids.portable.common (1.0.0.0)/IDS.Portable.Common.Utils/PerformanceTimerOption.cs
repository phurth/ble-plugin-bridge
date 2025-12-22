using System;

namespace IDS.Portable.Common.Utils
{
	[Flags]
	public enum PerformanceTimerOption
	{
		OnShowStart = 1,
		OnShowStopTotalTimeInMs = 2,
		AutoStartOnCreate = 4,
		None = 0,
		Verbose = 3
	}
}
