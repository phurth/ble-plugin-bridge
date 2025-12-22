namespace OneControl.Devices
{
	public enum GeneratorState
	{
		Off = 0,
		Priming = 1,
		Starting = 2,
		Running = 3,
		Stopping = 4,
		Unknown = 257,
		Offline = 256
	}
}
