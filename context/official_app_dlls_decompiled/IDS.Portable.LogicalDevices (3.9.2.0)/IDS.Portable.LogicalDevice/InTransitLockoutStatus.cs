namespace IDS.Portable.LogicalDevice
{
	public enum InTransitLockoutStatus
	{
		Unknown,
		Off,
		OnEnforced,
		OnIgnored,
		OnSomeOperationsAllowed,
		OnRemote
	}
}
