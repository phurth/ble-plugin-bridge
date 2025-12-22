using System;

namespace OneControl.Devices.TPMS
{
	[Flags]
	public enum TpmsGroupIdHasActiveFault : byte
	{
		None = 0,
		GroupId0 = 1,
		GroupId1 = 2,
		GroupId2 = 4,
		GroupId3 = 8,
		Reserved4 = 0x10,
		Reserved5 = 0x20,
		Reserved6 = 0x40,
		Reserved7 = 0x80
	}
}
