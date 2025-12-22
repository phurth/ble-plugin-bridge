using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayCapabilityType2 : LogicalDeviceCapability, ILogicalDeviceRelayCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		public const int PhysicalSwitchBitShift = 3;

		public const int AllLightsGroupBehaviorBitShift = 5;

		public LogicalDeviceCapabilitySerializable LogicalDeviceCapabilitySoftwareConfigurableFuseSerializable = $"{RelayCapabilityFlagType2.SupportsSoftwareConfigurableFuse}";

		public LogicalDeviceCapabilitySerializable LogicalDeviceCapabilityCoarsePositionSerializable = $"{RelayCapabilityFlagType2.SupportsCoarsePosition}";

		public LogicalDeviceCapabilitySerializable LogicalDeviceCapabilityFinePositionSerializable = $"{RelayCapabilityFlagType2.SupportsFinePosition}";

		protected RelayCapabilityFlagType2 CapabilityFlag => (RelayCapabilityFlagType2)RawValue;

		public bool IsSoftwareConfigurableFuseSupported => CapabilityFlag.HasFlag(RelayCapabilityFlagType2.SupportsSoftwareConfigurableFuse);

		public bool IsCoarsePositionSupported => CapabilityFlag.HasFlag(RelayCapabilityFlagType2.SupportsCoarsePosition);

		public bool AreAutoCommandsSupported => false;

		public bool IsFinePositionSupported => CapabilityFlag.HasFlag(RelayCapabilityFlagType2.SupportsFinePosition);

		public bool IsHomingSupported => false;

		public bool IsAwningSensorSupported => false;

		public PhysicalSwitchTypeCapability PhysicalSwitchType => (PhysicalSwitchTypeCapability)((RawValue & 0x18) >> 3);

		public AllLightsGroupBehaviorCapability AllLightsGroupBehavior => (AllLightsGroupBehaviorCapability)((RawValue & 0x60) >> 5);

		public override IEnumerable<LogicalDeviceCapabilitySerializable> ActiveCapabilities
		{
			[IteratorStateMachine(typeof(_003Cget_ActiveCapabilities_003Ed__28))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				return new _003Cget_ActiveCapabilities_003Ed__28(-2)
				{
					_003C_003E4__this = this
				};
			}
		}

		public LogicalDeviceRelayCapabilityType2(byte? rawCapability)
			: base(rawCapability)
		{
		}

		public LogicalDeviceRelayCapabilityType2(RelayCapabilityFlagType2 capabilityFlags)
			: this((byte)capabilityFlags)
		{
		}

		public LogicalDeviceRelayCapabilityType2()
			: this(RelayCapabilityFlagType2.None)
		{
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("IsSoftwareConfigurableFuseSupported");
			NotifyPropertyChanged("IsCoarsePositionSupported");
			NotifyPropertyChanged("IsFinePositionSupported");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
