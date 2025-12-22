namespace OneControl.Devices
{
	public enum PhysicalSwitchTypeCapability : byte
	{
		None = 0,
		Dimmable = 1,
		Toggle = 2,
		Momentary = 3,
		Unknown = byte.MaxValue
	}
}
