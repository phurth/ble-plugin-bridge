using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightDimmableCapability : LogicalDeviceCapability, ILogicalDeviceLightDimmableCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		private const byte ExtendedStatusSupportedBitmask = 4;

		private static readonly BitPositionValue SimulateOnOffSwitchStyleLightBitPosition = new BitPositionValue(24u);

		private const byte RgbGangableBitmask = 32;

		private static readonly BitPositionValue PhysicalSwitchTypeBitPosition = new BitPositionValue(192u);

		private static readonly BitPositionValue AllLightsGroupFeaturePosition = new BitPositionValue(3u);

		public readonly LogicalDeviceCapabilitySerializable LogicalDeviceCapabilityDimmableGangableSerializable = "RgbGangable";

		public readonly LogicalDeviceCapabilitySerializable LogicalDeviceCapabilityDimmableExtendedStatusSupportedSerializable = "ExtendedStatusSupported";

		public bool IsExtendedStatusSupported => (RawValue & 4) == 4;

		public bool RgbGangable => (RawValue & 0x20) == 32;

		public SimulatedOnOffStyleLightCapability SimulatedOnOffStyleLight => (SimulatedOnOffStyleLightCapability)SimulateOnOffSwitchStyleLightBitPosition.DecodeValue(RawValue);

		public PhysicalSwitchTypeCapability PhysicalSwitchType => (PhysicalSwitchTypeCapability)PhysicalSwitchTypeBitPosition.DecodeValue(RawValue);

		public AllLightsGroupBehaviorCapability AllLightsGroupBehavior => (AllLightsGroupBehaviorCapability)AllLightsGroupFeaturePosition.DecodeValue(RawValue);

		public override IEnumerable<LogicalDeviceCapabilitySerializable> ActiveCapabilities
		{
			[IteratorStateMachine(typeof(_003Cget_ActiveCapabilities_003Ed__23))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				return new _003Cget_ActiveCapabilities_003Ed__23(-2)
				{
					_003C_003E4__this = this
				};
			}
		}

		public LogicalDeviceLightDimmableCapability(byte? rawCapabilities)
			: base(rawCapabilities)
		{
		}

		public LogicalDeviceLightDimmableCapability(SimulatedOnOffStyleLightCapability onOffStyle, PhysicalSwitchTypeCapability physicalSwitchType, bool rgbGangable = false, bool extendedStatusSupported = false, AllLightsGroupBehaviorCapability allLightsGroupBehavior = AllLightsGroupBehaviorCapability.FeatureNotSupported)
			: this(MakeRawCapability(onOffStyle, physicalSwitchType, rgbGangable, extendedStatusSupported, allLightsGroupBehavior))
		{
		}

		public LogicalDeviceLightDimmableCapability()
			: this(0)
		{
		}

		private static byte MakeRawCapability(SimulatedOnOffStyleLightCapability onOffStyle, PhysicalSwitchTypeCapability physicalSwitchType, bool rgbGangable = false, bool extendedStatusSupported = false, AllLightsGroupBehaviorCapability allLightsGroupBehavior = AllLightsGroupBehaviorCapability.FeatureNotSupported)
		{
			byte b = 0;
			b = (byte)(b | (byte)SimulateOnOffSwitchStyleLightBitPosition.EncodeValue((uint)onOffStyle));
			b = (byte)(b | (byte)PhysicalSwitchTypeBitPosition.EncodeValue((uint)physicalSwitchType));
			b = (byte)(b | (byte)AllLightsGroupFeaturePosition.EncodeValue((uint)allLightsGroupBehavior));
			if (rgbGangable)
			{
				b = (byte)(b | 0x20u);
			}
			if (extendedStatusSupported)
			{
				b = (byte)(b | 4u);
			}
			return b;
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("SimulatedOnOffStyleLight");
			NotifyPropertyChanged("RgbGangable");
			NotifyPropertyChanged("IsExtendedStatusSupported");
			NotifyPropertyChanged("PhysicalSwitchType");
			NotifyPropertyChanged("AllLightsGroupBehavior");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
