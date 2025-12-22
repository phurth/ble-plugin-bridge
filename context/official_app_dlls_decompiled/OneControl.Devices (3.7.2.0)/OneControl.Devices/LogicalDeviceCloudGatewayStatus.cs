using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceCloudGatewayStatus : LogicalDeviceStatusPacketMutable
	{
		private const BasicBitMask LanOnlineBitMask = BasicBitMask.BitMask0X01;

		private const BasicBitMask InternetDetectedBitmask = BasicBitMask.BitMask0X02;

		private static readonly BitPositionValue ConnectStatusBitPosition = new BitPositionValue(12u);

		public const int MinimumStatusPacketSize = 1;

		public const uint StatusByteIndex = 0u;

		private const int ConnectionStatusShift = 2;

		public bool IsCloudConnectedToLan => GetBit(BasicBitMask.BitMask0X01);

		public bool IsCloudConnectedToWan => GetBit(BasicBitMask.BitMask0X02);

		public CloudConnectionStatus CloudConnectionStatus => (CloudConnectionStatus)GetValue(ConnectStatusBitPosition);

		public LogicalDeviceCloudGatewayStatus()
			: base(1u)
		{
		}

		internal void SetCloudConnectionStatus(CloudConnectionStatus cloudConnectionStatus)
		{
			SetValue(base.Data[0], ConnectStatusBitPosition);
		}

		internal void SetCloudConnectedToLan(bool connectedToLan)
		{
			SetBit(BasicBitMask.BitMask0X01, connectedToLan);
		}

		internal void SetCloudConnectedToWan(bool connectedToWan)
		{
			SetBit(BasicBitMask.BitMask0X02, connectedToWan);
		}
	}
}
