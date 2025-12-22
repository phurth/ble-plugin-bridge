using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDevicePowerMonitorDcCapability : LogicalDeviceCapability, ILogicalDevicePowerMonitorDcCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		private PowerMonitorDcCapabilityFlag _capabilityFlag => (PowerMonitorDcCapabilityFlag)RawValue;

		public bool IsBatteryCapacityAmpHoursSupported => _capabilityFlag.HasFlag(PowerMonitorDcCapabilityFlag.SupportsBatteryCapacityAmpHours);

		public LogicalDevicePowerMonitorDcCapability(byte? rawCapability)
		{
			UpdateDeviceCapability(rawCapability);
		}

		public LogicalDevicePowerMonitorDcCapability(ClimateZoneCapabilityFlag capabilityFlags)
		{
			UpdateDeviceCapability((byte)capabilityFlags);
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("IsBatteryCapacityAmpHoursSupported");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
