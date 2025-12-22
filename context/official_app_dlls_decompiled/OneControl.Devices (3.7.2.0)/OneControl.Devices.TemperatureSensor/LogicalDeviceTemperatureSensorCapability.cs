using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.TemperatureSensor
{
	public class LogicalDeviceTemperatureSensorCapability : LogicalDeviceCapability, ILogicalDeviceTemperatureSensorCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		public bool IsCoinCellBatterySupported => ((TemperatureSensorSupportedCapabilities)RawValue).HasFlag(TemperatureSensorSupportedCapabilities.CoinCellBattery);

		public bool IsHighTemperatureAlertSupported => ((TemperatureSensorSupportedCapabilities)RawValue).HasFlag(TemperatureSensorSupportedCapabilities.TemperatureHighAlert);

		public bool IsLowTemperatureAlertSupported => ((TemperatureSensorSupportedCapabilities)RawValue).HasFlag(TemperatureSensorSupportedCapabilities.TemperatureLowAlert);

		public bool IsLowBatteryAlertSupported => ((TemperatureSensorSupportedCapabilities)RawValue).HasFlag(TemperatureSensorSupportedCapabilities.BatteryAlert);

		public bool IsTemperatureInRangeAlertSupported => ((TemperatureSensorSupportedCapabilities)RawValue).HasFlag(TemperatureSensorSupportedCapabilities.TemperatureInRangeAlert);

		public bool IsBatteryCapacityReportingSupported => ((TemperatureSensorSupportedCapabilities)RawValue).HasFlag(TemperatureSensorSupportedCapabilities.BatteryCapacityReporting);

		public LogicalDeviceTemperatureSensorCapability(TemperatureSensorSupportedCapabilities capabilityFlags)
		{
			UpdateDeviceCapability((byte)capabilityFlags);
		}

		public LogicalDeviceTemperatureSensorCapability(byte? rawCapabilities)
		{
			UpdateDeviceCapability(rawCapabilities);
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("IsCoinCellBatterySupported");
			NotifyPropertyChanged("IsHighTemperatureAlertSupported");
			NotifyPropertyChanged("IsLowTemperatureAlertSupported");
			NotifyPropertyChanged("IsLowBatteryAlertSupported");
			NotifyPropertyChanged("IsTemperatureInRangeAlertSupported");
			NotifyPropertyChanged("IsBatteryCapacityReportingSupported");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
