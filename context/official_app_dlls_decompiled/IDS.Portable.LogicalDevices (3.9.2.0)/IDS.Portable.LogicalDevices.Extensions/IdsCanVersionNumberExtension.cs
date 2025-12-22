using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevices.Extensions
{
	public static class IdsCanVersionNumberExtension
	{
		public static bool IsInMotionLockoutSupported(this IDS_CAN_VERSION_NUMBER version)
		{
			if (version != IDS_CAN_VERSION_NUMBER.UNKNOWN)
			{
				return (byte)version >= 17;
			}
			return false;
		}

		public static bool HazardousStatusSupported(this IDS_CAN_VERSION_NUMBER version)
		{
			if (version != IDS_CAN_VERSION_NUMBER.UNKNOWN)
			{
				return (byte)version >= 18;
			}
			return false;
		}
	}
}
