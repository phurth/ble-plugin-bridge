namespace IDS.Portable.LogicalDevice
{
	public enum MonitorPanelControlType : byte
	{
		None = 0,
		MomentarySwitch = 1,
		LatchingSwitch = 2,
		SupplyTankIndicator = 3,
		WasteTankIndicator = 4,
		Invalid = byte.MaxValue
	}
}
