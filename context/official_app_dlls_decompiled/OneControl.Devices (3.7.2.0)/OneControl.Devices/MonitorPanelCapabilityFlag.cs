using System;

namespace OneControl.Devices
{
	[Flags]
	public enum MonitorPanelCapabilityFlag : byte
	{
		None = 0,
		HasBlePairingButton = 1,
		HasHighResolutionTankSupport = 2
	}
}
