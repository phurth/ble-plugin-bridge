using System;
using System.Collections.Generic;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public static class FirmwareUpdateOptionExtension
	{
		public const uint AccessoryGatewayEsp32Address = 3276800u;

		public const uint RvLinkBleGatewayEsp32Address = 3276800u;

		public const uint Leveler5S12Address = 163840u;

		public const uint Leveler5TouchpadEsp32Address = 4325376u;

		public const uint AbsS32Address = 131072u;

		public const uint SwayMk10Address = 40960u;

		public const uint SuperPremiumMonitorPanelAddress = 4325376u;

		public static bool TryGetUInt32(this FirmwareUpdateOption option, IReadOnlyDictionary<FirmwareUpdateOption, object> optionsDict, out uint value)
		{
			try
			{
				if (!optionsDict.TryGetValue(option, out var value2))
				{
					value = 0u;
					return false;
				}
				value = Convert.ToUInt32(value2);
				return true;
			}
			catch
			{
				value = 0u;
				return false;
			}
		}

		public static bool TryGetBool(this FirmwareUpdateOption option, IReadOnlyDictionary<FirmwareUpdateOption, object> optionsDict, out bool value)
		{
			try
			{
				if (!optionsDict.TryGetValue(option, out var value2))
				{
					value = false;
					return false;
				}
				value = Convert.ToUInt32(value2) != 0;
				return true;
			}
			catch
			{
				value = false;
				return false;
			}
		}

		public static bool IsDeviceAuthorizationRequired(this IReadOnlyDictionary<FirmwareUpdateOption, object> optionsDict)
		{
			if (!FirmwareUpdateOption.DeviceAuthorizationRequired.TryGetBool(optionsDict, out var value))
			{
				return false;
			}
			return value;
		}

		public static void SetDeviceAuthorizationRequired(this IDictionary<FirmwareUpdateOption, object> optionsDict, bool required)
		{
			try
			{
				optionsDict[FirmwareUpdateOption.DeviceAuthorizationRequired] = required;
			}
			catch
			{
			}
		}

		public static bool TryGetStartAddress(this IReadOnlyDictionary<FirmwareUpdateOption, object> optionsDict, out uint value)
		{
			return FirmwareUpdateOption.StartAddress.TryGetUInt32(optionsDict, out value);
		}

		public static void SetStartAddress(this IDictionary<FirmwareUpdateOption, object> optionsDict, uint value)
		{
			try
			{
				optionsDict[FirmwareUpdateOption.StartAddress] = value;
			}
			catch
			{
			}
		}

		public static bool TryGetJumpToBootHoldTime(this IReadOnlyDictionary<FirmwareUpdateOption, object> optionsDict, out TimeSpan holdTime)
		{
			if (!FirmwareUpdateOption.JumpToBootHoldTimeMs.TryGetUInt32(optionsDict, out var value))
			{
				holdTime = TimeSpan.Zero;
				return false;
			}
			holdTime = TimeSpan.FromMilliseconds(value);
			return true;
		}

		public static void SetJumpToBootHoldTime(this IDictionary<FirmwareUpdateOption, object> optionsDict, TimeSpan holdTime)
		{
			try
			{
				optionsDict[FirmwareUpdateOption.JumpToBootHoldTimeMs] = holdTime.TotalMilliseconds;
			}
			catch
			{
			}
		}
	}
}
