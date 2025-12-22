namespace OneControl.Devices
{
	public enum LogicalDeviceLevelerStatusIndicatorStateType3
	{
		[LogicalDeviceLevelerStatusIndicatorActiveType3(false)]
		Unknown = 65535,
		[LogicalDeviceLevelerStatusIndicatorActiveType3(false)]
		Off = 0,
		[LogicalDeviceLevelerStatusIndicatorActiveType3(true)]
		Blink = 1,
		[LogicalDeviceLevelerStatusIndicatorActiveType3(true)]
		InverseBlink = 2,
		[LogicalDeviceLevelerStatusIndicatorActiveType3(true)]
		On = 3
	}
}
