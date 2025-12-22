using System;

namespace OneControl.Devices
{
	[Flags]
	public enum LevelerJackDirection : byte
	{
		None = 0,
		Extend = 1,
		Retract = 2,
		Both = 3
	}
}
