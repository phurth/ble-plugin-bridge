namespace OneControl.Devices
{
	public enum HBridgeCommand : byte
	{
		Stop = 0,
		Forward = 1,
		Reverse = 2,
		ClearUserClearRequiredLatch = 3,
		HomeReset = 4,
		AutoForward = 5,
		AutoReverse = 6,
		Unknown = byte.MaxValue
	}
}
