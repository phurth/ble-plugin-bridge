using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.OneControlTouchPanel
{
	public class LogicalDeviceOneControlTouchPanelCapability : LogicalDeviceCapability, ILogicalDeviceOneControlTouchPanelCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		private OneControlTouchPanelCapabilityFlag _capabilityFlag => (OneControlTouchPanelCapabilityFlag)RawValue;

		public bool IsHighResolutionTanksSupported => _capabilityFlag.HasFlag(OneControlTouchPanelCapabilityFlag.SupportsHighResolutionTanks);

		public bool HasStaticMacAddress => _capabilityFlag.HasFlag(OneControlTouchPanelCapabilityFlag.HasStaticMacAddress);

		public LogicalDeviceOneControlTouchPanelCapability(byte? rawCapability)
			: base(rawCapability)
		{
			UpdateDeviceCapability(rawCapability);
		}

		public LogicalDeviceOneControlTouchPanelCapability(OneControlTouchPanelCapabilityFlag capabilityFlags)
			: this((byte)capabilityFlags)
		{
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("IsHighResolutionTanksSupported");
			NotifyPropertyChanged("HasStaticMacAddress");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
