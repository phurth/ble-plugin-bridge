using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeCapabilityType2 : LogicalDeviceCapability, ILogicalDeviceRelayCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		public const int PhysicalSwitchBitShift = 3;

		protected RelayHBridgeCapabilityFlagType2 CapabilityFlag => (RelayHBridgeCapabilityFlagType2)RawValue;

		public bool IsSoftwareConfigurableFuseSupported => CapabilityFlag.HasFlag(RelayHBridgeCapabilityFlagType2.SupportsSoftwareConfigurableFuse);

		public bool IsCoarsePositionSupported
		{
			get
			{
				if (AreAutoCommandsSupported)
				{
					return !IsFinePositionSupported;
				}
				return false;
			}
		}

		public bool AreAutoCommandsSupported => CapabilityFlag.HasFlag(RelayHBridgeCapabilityFlagType2.SupportsAutoCommands);

		public bool IsFinePositionSupported => CapabilityFlag.HasFlag(RelayHBridgeCapabilityFlagType2.SupportsFinePosition);

		public bool IsHomingSupported => CapabilityFlag.HasFlag(RelayHBridgeCapabilityFlagType2.SupportsHoming);

		public bool IsAwningSensorSupported => CapabilityFlag.HasFlag(RelayHBridgeCapabilityFlagType2.SupportsAwningSensor);

		public PhysicalSwitchTypeCapability PhysicalSwitchType => (PhysicalSwitchTypeCapability)((RawValue & 0x18) >> 3);

		public AllLightsGroupBehaviorCapability AllLightsGroupBehavior => AllLightsGroupBehaviorCapability.FeatureNotSupported;

		public LogicalDeviceRelayHBridgeCapabilityType2(byte? rawCapability)
		{
			UpdateDeviceCapability(rawCapability);
		}

		public LogicalDeviceRelayHBridgeCapabilityType2(RelayHBridgeCapabilityFlagType2 capabilityFlags)
		{
			UpdateDeviceCapability((byte)capabilityFlags);
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("IsSoftwareConfigurableFuseSupported");
			NotifyPropertyChanged("AreAutoCommandsSupported");
			NotifyPropertyChanged("IsFinePositionSupported");
			NotifyPropertyChanged("IsHomingSupported");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
