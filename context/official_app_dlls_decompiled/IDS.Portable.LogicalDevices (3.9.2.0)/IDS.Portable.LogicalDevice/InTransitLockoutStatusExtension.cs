namespace IDS.Portable.LogicalDevice
{
	public static class InTransitLockoutStatusExtension
	{
		public static bool IsInLockout(this InTransitLockoutStatus lockout)
		{
			switch (lockout)
			{
			case InTransitLockoutStatus.OnEnforced:
			case InTransitLockoutStatus.OnRemote:
				return true;
			default:
				return false;
			}
		}
	}
}
