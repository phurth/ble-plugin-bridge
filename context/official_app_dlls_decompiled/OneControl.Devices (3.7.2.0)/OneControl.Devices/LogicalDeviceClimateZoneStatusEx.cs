using IDS.Core.IDS_CAN;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceClimateZoneStatusEx : LogicalDeviceStatusPacketMutableExtended
	{
		private const int MinimumStatusPacketSize = 2;

		public const int UserMessageDtcIndex = 0;

		public DTC_ID UserMessageDtc => (DTC_ID)base.Data.GetValueUInt16(0);

		public void SetUserMessageDtc(DTC_ID userMessageDtc)
		{
			base.Data.SetValueUInt16((ushort)userMessageDtc, 0);
		}

		public LogicalDeviceClimateZoneStatusEx()
			: base(2u)
		{
		}

		public LogicalDeviceClimateZoneStatusEx(DTC_ID userMessageDtc)
		{
			SetUserMessageDtc(userMessageDtc);
		}

		public LogicalDeviceClimateZoneStatusEx(LogicalDeviceClimateZoneStatusEx originalStatus)
		{
			byte[] data = originalStatus.Data;
			Update(data, (uint)data.Length, originalStatus.ExtendedByte);
		}

		public override string ToString()
		{
			return $"{UserMessageDtc}";
		}
	}
}
