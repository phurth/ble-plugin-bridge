namespace OneControl.Devices
{
	public enum LogicalDeviceLevelerStatusIndicatorStateType1
	{
		[LogicalDeviceLevelerStatusIndicatorActiveType1(false)]
		Unknown = 65535,
		[LogicalDeviceLevelerStatusIndicatorActiveType1(false)]
		Off = 0,
		[LogicalDeviceLevelerStatusIndicatorActiveType1(true)]
		OnSolid = 1,
		[LogicalDeviceLevelerStatusIndicatorActiveType1(true)]
		[LogicalDeviceLevelerIndicatorBlink(1000u, 0.5, false)]
		OnBlinkHalfHz = 2,
		[LogicalDeviceLevelerStatusIndicatorActiveType1(true)]
		[LogicalDeviceLevelerIndicatorBlink(500u, 0.5, false)]
		OnBlinkOneHz = 3,
		[LogicalDeviceLevelerStatusIndicatorActiveType1(true)]
		[LogicalDeviceLevelerIndicatorBlink(250u, 0.5, false)]
		OnBlinkTwoHz = 4,
		[LogicalDeviceLevelerStatusIndicatorActiveType1(true)]
		[LogicalDeviceLevelerIndicatorBlink(125u, 0.5, false)]
		OnBlinkFourHz = 5,
		[LogicalDeviceLevelerStatusIndicatorActiveType1(true)]
		[LogicalDeviceLevelerIndicatorBlink(500u, 0.2, false)]
		OnBlinkBriefOn = 6,
		[LogicalDeviceLevelerStatusIndicatorActiveType1(true)]
		[LogicalDeviceLevelerIndicatorBlink(500u, 0.8, true)]
		OnBlinkBriefOff = 7
	}
}
