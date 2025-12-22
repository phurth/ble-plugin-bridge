using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Devices
{
	public static class LogicalDeviceLevelerScreenType4Extension
	{
		public static LogicalDeviceLevelerOperationAutoType4 ToOperationAuto(this LogicalDeviceLevelerScreenType4 screen)
		{
			if (screen.GetAttribute<LogicalDeviceLevelerOperationAutoType4Attribute>() == null)
			{
				return LogicalDeviceLevelerOperationAutoType4.Unknown;
			}
			return Enum<LogicalDeviceLevelerOperationAutoType4>.TryConvert((int)screen);
		}

		public static bool IsOperationAuto(this LogicalDeviceLevelerScreenType4 screen)
		{
			return screen.ToOperationAuto() != LogicalDeviceLevelerOperationAutoType4.Unknown;
		}

		public static LogicalDeviceLevelerOperationManualType4 ToOperationManual(this LogicalDeviceLevelerScreenType4 screen)
		{
			if (screen.GetAttribute<LogicalDeviceLevelerOperationManualType4Attribute>() == null)
			{
				return LogicalDeviceLevelerOperationManualType4.Unknown;
			}
			return Enum<LogicalDeviceLevelerOperationManualType4>.TryConvert((int)screen);
		}

		public static bool IsOperationManual(this LogicalDeviceLevelerScreenType4 screen)
		{
			return screen.ToOperationManual() != LogicalDeviceLevelerOperationManualType4.Unknown;
		}

		public static bool IsPrompt(this LogicalDeviceLevelerScreenType4 screen)
		{
			if (screen != LogicalDeviceLevelerScreenType4.PromptAirbagTimeSelect && screen != LogicalDeviceLevelerScreenType4.PromptFault && screen != LogicalDeviceLevelerScreenType4.PromptInfo)
			{
				return screen == LogicalDeviceLevelerScreenType4.PromptYesNo;
			}
			return true;
		}

		public static bool HasConsole(this LogicalDeviceLevelerScreenType4 screen)
		{
			return screen.GetAttribute<LogicalDeviceLevelerOperationManualType4Attribute>()?.HasConsole ?? false;
		}

		public static bool HasFault(this LogicalDeviceLevelerScreenType4 screen)
		{
			return screen.GetAttribute<LogicalDeviceLevelerOperationManualType4Attribute>()?.HasFault ?? false;
		}
	}
}
