using IDS.Core.Types;

namespace IDS.Portable.LogicalDevice
{
	public static class PidAddressExtension
	{
		public static UInt8 PidAddressValueToByte(uint rawPidValue)
		{
			return (byte)rawPidValue;
		}

		public static ushort PidAddressValueToUInt16(uint rawPidValue)
		{
			return (ushort)rawPidValue;
		}

		public static uint PidAddressValueToUInt32(uint rawPidValue)
		{
			return rawPidValue;
		}
	}
}
