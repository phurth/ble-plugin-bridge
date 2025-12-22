namespace OneControl.Devices
{
	public static class LogicalDeviceLevelerButtonJackMovementFaultType4Extension
	{
		internal static LogicalDeviceLevelerJackMovementType4 ToJackMovement(this LogicalDeviceLevelerButtonJackMovementFaultManualType4 button)
		{
			return (LogicalDeviceLevelerJackMovementType4)(button & ~LogicalDeviceLevelerButtonJackMovementFaultManualType4.AutoRetract);
		}
	}
}
