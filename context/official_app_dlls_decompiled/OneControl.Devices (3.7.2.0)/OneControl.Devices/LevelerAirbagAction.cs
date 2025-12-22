using System;

namespace OneControl.Devices
{
	[Flags]
	public enum LevelerAirbagAction
	{
		None = 0,
		Fill = 1,
		Dump = 2,
		Both = 0x11
	}
}
