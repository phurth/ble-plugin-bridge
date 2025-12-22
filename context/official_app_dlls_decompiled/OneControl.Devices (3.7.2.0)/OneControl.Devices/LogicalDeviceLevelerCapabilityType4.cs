using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Leveler;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCapabilityType4 : LogicalDeviceCapability
	{
		private const byte JackPositionSupportedBitmask = 1;

		private static readonly BitPositionValue JackConfigurationBitPosition = new BitPositionValue(14u);

		private static readonly BitPositionValue ChassisConfigurationBitPosition = new BitPositionValue(48u);

		private const int JackSupportedStartingBitIndex = 0;

		private const int JackConfigurationStartingBitIndex = 1;

		private const int ChassisConfigurationStartingBitIndex = 4;

		public bool IsJackPositionSupported => (RawValue & 1) != 0;

		public LevelerConfigurationJack JackConfiguration => (LevelerConfigurationJack)JackConfigurationBitPosition.DecodeValue(RawValue);

		public LevelerConfigurationChassis Chassis => (LevelerConfigurationChassis)ChassisConfigurationBitPosition.DecodeValue(RawValue);

		private LogicalDeviceLevelerCapabilityType4(byte rawCapability)
		{
			UpdateDeviceCapability(rawCapability);
		}

		public LogicalDeviceLevelerCapabilityType4(byte? rawCapability)
			: this(rawCapability.GetValueOrDefault())
		{
		}

		public LogicalDeviceLevelerCapabilityType4(LevelerConfigurationChassis chassisType, LevelerConfigurationJack jackType, bool isJackPositionSupported)
			: this(MakeRawCapability(chassisType, jackType, isJackPositionSupported))
		{
		}

		private static byte MakeRawCapability(LevelerConfigurationChassis chassisType, LevelerConfigurationJack jackType, bool isJackPositionSupported)
		{
			return (byte)((isJackPositionSupported ? 1u : 0u) | ((uint)(jackType & (LevelerConfigurationJack)7) << 1) | ((uint)(chassisType & LevelerConfigurationChassis.TravelTrailer) << 4));
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("IsJackPositionSupported");
			NotifyPropertyChanged("JackConfiguration");
			NotifyPropertyChanged("Chassis");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
