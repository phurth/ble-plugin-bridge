using System;

namespace OneControl.Devices.OneControlTouchPanel
{
	[Flags]
	public enum OneControlTouchPanelCapabilityFlag : byte
	{
		None = 0,
		SupportsHighResolutionTanks = 1,
		HasStaticMacAddress = 2,
		Reserved2 = 4,
		Reserved3 = 8,
		Reserved4 = 0x10,
		Reserved5 = 0x20,
		Reserved6 = 0x40,
		Reserved7 = 0x80
	}
}
