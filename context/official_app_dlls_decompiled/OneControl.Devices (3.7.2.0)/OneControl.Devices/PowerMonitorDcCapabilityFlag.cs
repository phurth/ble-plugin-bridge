using System;

namespace OneControl.Devices
{
	[Flags]
	public enum PowerMonitorDcCapabilityFlag : byte
	{
		None = 0,
		SupportsBatteryCapacityAmpHours = 1
	}
}
