using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Devices
{
	public static class LogicalDeviceLevelerOperationManualType4Extension
	{
		public static LogicalDeviceLevelerScreenType4 ToScreen(this LogicalDeviceLevelerOperationManualType4 operationManual)
		{
			LogicalDeviceLevelerScreenType4 logicalDeviceLevelerScreenType = Enum<LogicalDeviceLevelerScreenType4>.TryConvert((int)operationManual);
			if (logicalDeviceLevelerScreenType == LogicalDeviceLevelerScreenType4.Unknown)
			{
				return logicalDeviceLevelerScreenType;
			}
			if (logicalDeviceLevelerScreenType.GetAttribute<LogicalDeviceLevelerOperationManualType4Attribute>() == null)
			{
				return LogicalDeviceLevelerScreenType4.Unknown;
			}
			return logicalDeviceLevelerScreenType;
		}
	}
}
