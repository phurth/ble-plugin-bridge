namespace OneControl.Devices
{
	public static class LogicalDeviceLevelerButtonJackMovementZeroType4Extension
	{
		internal static LogicalDeviceLevelerJackMovementType4 ToJackMovement(this LogicalDeviceLevelerButtonJackMovementZeroType4 button)
		{
			return (LogicalDeviceLevelerJackMovementType4)(button & ~LogicalDeviceLevelerButtonJackMovementZeroType4.SetZeroPoint);
		}
	}
}
