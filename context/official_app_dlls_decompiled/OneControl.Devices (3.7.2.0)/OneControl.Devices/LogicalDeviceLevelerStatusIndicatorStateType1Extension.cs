using IDS.Portable.Common.Extensions;

namespace OneControl.Devices
{
	public static class LogicalDeviceLevelerStatusIndicatorStateType1Extension
	{
		public static bool IsActive(this LogicalDeviceLevelerStatusIndicatorStateType1 indicatorStateType1)
		{
			return indicatorStateType1.GetAttributeValue<LogicalDeviceLevelerStatusIndicatorActiveType1Attribute, bool>(defaultValue: false);
		}
	}
}
