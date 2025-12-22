using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Devices
{
	public static class LogicalDeviceLevelerOperationAutoType4Extension
	{
		public static LogicalDeviceLevelerScreenType4 ToScreen(this LogicalDeviceLevelerOperationAutoType4 operationAuto)
		{
			LogicalDeviceLevelerScreenType4 logicalDeviceLevelerScreenType = Enum<LogicalDeviceLevelerScreenType4>.TryConvert((int)operationAuto);
			if (logicalDeviceLevelerScreenType == LogicalDeviceLevelerScreenType4.Unknown)
			{
				return logicalDeviceLevelerScreenType;
			}
			if (logicalDeviceLevelerScreenType.GetAttribute<LogicalDeviceLevelerOperationAutoType4Attribute>() == null)
			{
				return LogicalDeviceLevelerScreenType4.Unknown;
			}
			return logicalDeviceLevelerScreenType;
		}
	}
}
