using System;

namespace OneControl.Devices
{
	[Flags]
	public enum LogicalDeviceLevelerButtonType1 : ushort
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
		Power = 0x200,
		MenuUp = 0x400
	}
}
