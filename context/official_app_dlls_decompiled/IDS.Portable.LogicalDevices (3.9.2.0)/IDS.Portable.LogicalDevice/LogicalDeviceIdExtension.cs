using IDS.Core.IDS_CAN;
using IDS.Core.Types;

namespace IDS.Portable.LogicalDevice
{
	public static class LogicalDeviceIdExtension
	{
		public enum UniqueId : ushort
		{
			IdsCanDevice,
			IdsCanDeviceFeature
		}

		public static UInt128 MakeCanDeviceUniqueId(MAC mac, PRODUCT_ID productId, DEVICE_TYPE deviceType, int deviceInstance)
		{
			return ((((((((((((((((((UInt128)mac[0] << 8) | mac[1]) << 8) | mac[2]) << 8) | mac[3]) << 8) | mac[4]) << 8) | mac[5]) << 16) | (ushort)productId) << 8) | (byte)deviceType) << 4) | (byte)((uint)deviceInstance & 0xFu)) << 4 << 16) | (ushort)0;
		}

		public static UInt128 MakeCanDeviceFeatureUniqueId(MAC mac, PRODUCT_ID productId, DEVICE_TYPE deviceType, int deviceInstance, FUNCTION_NAME functionName, int functionInstance)
		{
			return ((((((((((((((((((((((UInt128)mac[0] << 8) | mac[1]) << 8) | mac[2]) << 8) | mac[3]) << 8) | mac[4]) << 8) | mac[5]) << 16) | (ushort)productId) << 8) | (byte)deviceType) << 4) | (byte)((uint)deviceInstance & 0xFu)) << 4) | (byte)((uint)functionInstance & 0xFu)) << 16) | (ushort)((ushort)functionName & 0xFFFFu)) << 16) | (ushort)1;
		}
	}
}
