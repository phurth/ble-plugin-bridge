using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceMonitorPanelStatus : LogicalDeviceStatusPacketMutable, ILogicalDeviceStatus<LogicalDeviceMonitorPanelStatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		private const int MinimumStatusPacketSize = 1;

		public const BasicBitMask DeviceConfigurationDataValidBitmask = BasicBitMask.BitMask0X01;

		public const BasicBitMask DeviceDefinitionsValidBitmask = BasicBitMask.BitMask0X02;

		public const int StatusByteIndex = 0;

		public bool IsDeviceConfigurationDataValid => GetBit(BasicBitMask.BitMask0X01, 0);

		public bool AreDeviceDefinitionsValid => GetBit(BasicBitMask.BitMask0X02, 0);

		internal byte StatusRaw => GetByte(byte.MaxValue, 0);

		public LogicalDeviceMonitorPanelStatus()
			: base(1u)
		{
		}

		public void SetIsDeviceConfigurationDataValid(bool valid)
		{
			SetBit(BasicBitMask.BitMask0X01, valid, 0);
		}

		public void SetAreDeviceDefinitionsValid(bool valid)
		{
			SetBit(BasicBitMask.BitMask0X02, valid, 0);
		}

		internal void SetStatusRaw(byte rawStatus)
		{
			SetByte(byte.MaxValue, rawStatus, 0);
		}

		public LogicalDeviceMonitorPanelStatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceMonitorPanelStatusSerializable(StatusRaw);
		}
	}
}
