using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	internal static class PidValueCheckExtension
	{
		internal static PidValueCheck PidCheckValueDefault(ulong value, IDevicePID? devicePid = null)
		{
			if (devicePid != null && !devicePid!.IsValueValid)
			{
				return PidValueCheck.NoValue;
			}
			return PidValueCheck.HasValue;
		}

		internal static PidValueCheck PidCheckValueDefault(uint value, IDevicePID? devicePid = null)
		{
			if (devicePid != null && !devicePid!.IsValueValid)
			{
				return PidValueCheck.NoValue;
			}
			return PidValueCheck.HasValue;
		}
	}
}
