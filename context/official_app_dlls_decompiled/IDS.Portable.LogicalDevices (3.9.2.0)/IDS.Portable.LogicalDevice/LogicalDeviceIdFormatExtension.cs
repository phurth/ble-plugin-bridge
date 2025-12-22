using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public static class LogicalDeviceIdFormatExtension
	{
		private const int RequiredIdStringSize = 34;

		public const int InstanceWhenLinkedToWirelessSwitch = 15;

		public static string FormatLogicalId(this LogicalDeviceIdFormat format, ILogicalDeviceId localDeviceId)
		{
			FUNCTION_NAME functionName = localDeviceId?.FunctionName ?? FUNCTION_NAME.UNKNOWN;
			int functionInstance = localDeviceId?.FunctionInstance ?? 0;
			switch (format)
			{
			case LogicalDeviceIdFormat.FunctionNameFullWithoutFunctionInstance:
				return RemoveFunctionInstanceHandleBar(functionName.ToFunctionName().GetName()) ?? "";
			case LogicalDeviceIdFormat.FunctionNameCommon:
				return FormatFunctionNameWithFunctionInstance(functionName, functionInstance, showLinkedToWirelessSwitchInstance: false);
			case LogicalDeviceIdFormat.FunctionNameShortCommon:
				return FormatFunctionNameShortWithFunctionInstance(functionName, functionInstance, showLinkedToWirelessSwitchInstance: false);
			case LogicalDeviceIdFormat.FunctionNameShortAbbreviatedCommon:
				return FormatFunctionNameShortAbbreviatedWithFunctionInstance(functionName, functionInstance, showLinkedToWirelessSwitchInstance: false);
			case LogicalDeviceIdFormat.Debug:
			{
				byte b = localDeviceId?.DeviceType ?? ((DEVICE_TYPE)(byte)0);
				int num = localDeviceId?.DeviceInstance ?? 0;
				bool flag = (uint)(b - 30) <= 3u;
				string text = (flag ? " Type 2 " : " ");
				string text2 = FormatFunctionNameWithFunctionInstance(functionName, functionInstance, showLinkedToWirelessSwitchInstance: true);
				return $"\"{b}{text}{num}, {text2}\"";
			}
			case LogicalDeviceIdFormat.LdiStringEncodingBase:
				return localDeviceId?.ToLdiStringEncoding().Substring(0, 34) ?? string.Empty;
			case LogicalDeviceIdFormat.LdiStringEncodingFull:
				return localDeviceId?.ToLdiStringEncoding() ?? string.Empty;
			default:
				return FormatFunctionNameWithFunctionInstance(functionName, functionInstance, showLinkedToWirelessSwitchInstance: true);
			}
		}

		public static string FormatFunctionNameWithFunctionInstance(FUNCTION_NAME functionName, int functionInstance, bool showLinkedToWirelessSwitchInstance)
		{
			return FormatFunctionNameStringWithFunctionInstance(functionName, functionName.ToFunctionName().GetName(), functionInstance, showLinkedToWirelessSwitchInstance);
		}

		public static string FormatFunctionNameShortWithFunctionInstance(FUNCTION_NAME functionName, int functionInstance, bool showLinkedToWirelessSwitchInstance)
		{
			return FormatFunctionNameStringWithFunctionInstance(functionName, functionName.ToFunctionName().GetNameShort(), functionInstance, showLinkedToWirelessSwitchInstance);
		}

		public static string FormatFunctionNameShortAbbreviatedWithFunctionInstance(FUNCTION_NAME functionName, int functionInstance, bool showLinkedToWirelessSwitchInstance)
		{
			return FormatFunctionNameStringWithFunctionInstance(functionName, functionName.ToFunctionName().GetNameShortAbbreviated(), functionInstance, showLinkedToWirelessSwitchInstance);
		}

		private static string FormatFunctionNameStringWithFunctionInstance(FUNCTION_NAME functionName, string functionNameStr, int functionInstance, bool showLinkedToWirelessSwitchInstance)
		{
			if (!functionName.IsValid || functionInstance == 0 || (functionInstance == 15 && !showLinkedToWirelessSwitchInstance))
			{
				return RemoveFunctionInstanceHandleBar(functionNameStr);
			}
			if (functionNameStr.Contains("{0}"))
			{
				try
				{
					return string.Format(functionNameStr, functionInstance);
				}
				catch
				{
					functionNameStr = RemoveFunctionInstanceHandleBar(functionNameStr);
				}
			}
			return $"{functionNameStr} {functionInstance}";
		}

		private static string RemoveFunctionInstanceHandleBar(string functionNameStr)
		{
			if (functionNameStr.Contains("{0} "))
			{
				return functionNameStr.Replace("{0} ", "");
			}
			if (functionNameStr.Contains("{0}"))
			{
				return functionNameStr.Replace("{0}", "");
			}
			return functionNameStr;
		}

		public static string MacAsHexString(this ILogicalDeviceId ldId)
		{
			return MacAsHexString(ldId.ProductMacAddress);
		}

		public static string MacAsHexString(MAC? mac)
		{
			if (!(mac == null))
			{
				return $"{mac![0]:X2}{mac![1]:X2}{mac![2]:X2}{mac![3]:X2}{mac![4]:X2}{mac![5]:X2}";
			}
			return "000000000000";
		}
	}
}
