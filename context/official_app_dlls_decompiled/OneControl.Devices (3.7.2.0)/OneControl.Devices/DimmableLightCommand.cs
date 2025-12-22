namespace OneControl.Devices
{
	public enum DimmableLightCommand : byte
	{
		Off = 0,
		On = 1,
		Blink = 2,
		Swell = 3,
		Settings = 126,
		Restore = 127
	}
}
