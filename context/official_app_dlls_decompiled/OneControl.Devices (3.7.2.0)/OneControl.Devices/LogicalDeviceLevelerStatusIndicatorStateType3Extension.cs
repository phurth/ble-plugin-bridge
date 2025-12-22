using IDS.Portable.Common.Extensions;

namespace OneControl.Devices
{
	public static class LogicalDeviceLevelerStatusIndicatorStateType3Extension
	{
		public static bool IsActive(this LogicalDeviceLevelerStatusIndicatorStateType3 indicatorStateType3)
		{
			return indicatorStateType3.GetAttributeValue<LogicalDeviceLevelerStatusIndicatorActiveType3Attribute, bool>(defaultValue: false);
		}
	}
}
