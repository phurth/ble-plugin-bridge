using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceMonitorPanelCapability : LogicalDeviceCapability, ILogicalDeviceMonitorPanelCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		private MonitorPanelCapabilityFlag CapabilityFlag => (MonitorPanelCapabilityFlag)RawValue;

		public bool HasBlePairingButton => CapabilityFlag.HasFlag(MonitorPanelCapabilityFlag.HasBlePairingButton);

		public bool SupportsHighResolutionTanks => CapabilityFlag.HasFlag(MonitorPanelCapabilityFlag.HasHighResolutionTankSupport);

		public LogicalDeviceMonitorPanelCapability()
			: this((byte?)(byte)0)
		{
		}

		public LogicalDeviceMonitorPanelCapability(byte? rawCapability)
		{
			UpdateDeviceCapability(rawCapability);
		}

		public LogicalDeviceMonitorPanelCapability(ClimateZoneCapabilityFlag capabilityFlags)
		{
			UpdateDeviceCapability((byte)capabilityFlags);
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("HasBlePairingButton");
			NotifyPropertyChanged("SupportsHighResolutionTanks");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
