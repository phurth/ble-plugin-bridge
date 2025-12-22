using System;

namespace OneControl.Devices
{
	[Flags]
	public enum LogicalDeviceLevelerButtonType3 : ushort
	{
		None = 0,
		Right = 1,
		Left = 2,
		Rear = 4,
		Front = 8,
		AutoLevel = 0x10,
		Retract = 0x20,
		Enter = 0x40,
		MenuDown = 0x80,
		Extend = 0x100,
		Back = 0x200,
		MenuUp = 0x400,
		AutoHitch = 0x800,
		EnterSetup = 0x1000,
		Reserved1 = 0x2000,
		Reserved2 = 0x4000,
		Reserved3 = 0x8000
	}
}
