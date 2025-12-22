using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayCapabilityType1 : LogicalDeviceCapability, ILogicalDeviceRelayCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		public LogicalDeviceCapabilitySerializable LogicalDeviceCapabilitySoftwareConfigurableFuseSerializable = $"{RelayCapabilityFlagType1.SupportsSoftwareConfigurableFuse}";

		private RelayCapabilityFlagType1 _capabilityFlag => (RelayCapabilityFlagType1)RawValue;

		public bool IsSoftwareConfigurableFuseSupported => _capabilityFlag.HasFlag(RelayCapabilityFlagType1.SupportsSoftwareConfigurableFuse);

		public bool IsCoarsePositionSupported => false;

		public bool AreAutoCommandsSupported => false;

		public bool IsFinePositionSupported => false;

		public bool IsHomingSupported => false;

		public bool IsAwningSensorSupported => false;

		public PhysicalSwitchTypeCapability PhysicalSwitchType => PhysicalSwitchTypeCapability.Unknown;

		public AllLightsGroupBehaviorCapability AllLightsGroupBehavior => AllLightsGroupBehaviorCapability.FeatureNotSupported;

		public override IEnumerable<LogicalDeviceCapabilitySerializable> ActiveCapabilities
		{
			[IteratorStateMachine(typeof(_003Cget_ActiveCapabilities_003Ed__24))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				return new _003Cget_ActiveCapabilities_003Ed__24(-2)
				{
					_003C_003E4__this = this
				};
			}
		}

		public LogicalDeviceRelayCapabilityType1(byte? rawCapability)
			: base(rawCapability)
		{
		}

		public LogicalDeviceRelayCapabilityType1(RelayCapabilityFlagType1 capabilityFlags)
			: this((byte)capabilityFlags)
		{
		}

		public LogicalDeviceRelayCapabilityType1()
			: this(RelayCapabilityFlagType1.None)
		{
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("IsSoftwareConfigurableFuseSupported");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
