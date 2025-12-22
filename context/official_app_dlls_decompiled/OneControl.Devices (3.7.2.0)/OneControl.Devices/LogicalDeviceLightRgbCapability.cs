using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightRgbCapability : LogicalDeviceCapability, ILogicalDeviceLightRgbCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		private enum LogicalDeviceLightRgbCapabilitiesBitShift
		{
			RgbGangable = 5,
			PhysicalSwitchType = 6,
			AllLightsGroupFeature = 3
		}

		private enum LogicalDeviceLightRgbCapabilitiesMask : byte
		{
			RgbGangable = 32,
			PhysicalSwitchType = 192,
			AllLightsGroupFeature = 24
		}

		public LogicalDeviceCapabilitySerializable LogicalDeviceCapabilityRgbGangableSerializable = "RgbUnGangable";

		public bool RgbUnGangable => (byte)((RawValue & 0x20) >> 5) > 0;

		public PhysicalSwitchTypeCapability PhysicalSwitchType => (PhysicalSwitchTypeCapability)((RawValue & 0xC0) >> 6);

		public AllLightsGroupBehaviorCapability AllLightsGroupBehavior => (AllLightsGroupBehaviorCapability)((RawValue & 0x18) >> 3);

		public override IEnumerable<LogicalDeviceCapabilitySerializable> ActiveCapabilities
		{
			[IteratorStateMachine(typeof(_003Cget_ActiveCapabilities_003Ed__13))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				return new _003Cget_ActiveCapabilities_003Ed__13(-2)
				{
					_003C_003E4__this = this
				};
			}
		}

		public LogicalDeviceLightRgbCapability(byte? rawCapabilities)
		{
		}

		public LogicalDeviceLightRgbCapability()
			: this(0)
		{
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("RgbUnGangable");
			NotifyPropertyChanged("PhysicalSwitchType");
			NotifyPropertyChanged("AllLightsGroupBehavior");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
