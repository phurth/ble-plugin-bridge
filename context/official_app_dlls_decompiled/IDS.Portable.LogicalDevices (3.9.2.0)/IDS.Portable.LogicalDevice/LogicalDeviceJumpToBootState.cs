namespace IDS.Portable.LogicalDevice
{
	public enum LogicalDeviceJumpToBootState : uint
	{
		Unknown = 0u,
		FeatureIdle = 287454020u,
		RequestBootLoaderWithHoldTime = 1432778632u,
		BootHoldInProgress = 2576980377u,
		RequestError = 2863315899u,
		SoftwareError = 3435978205u,
		MemoryError = 4008640511u
	}
}
