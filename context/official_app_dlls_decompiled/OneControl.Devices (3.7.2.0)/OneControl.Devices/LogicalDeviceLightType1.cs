using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightType1 : LogicalDeviceRelayBasicLatchingType1, ILogicalDeviceLatchingRelayLight, ILogicalDeviceSwitchableLight, ILogicalDeviceSwitchable, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ISwitchableDevice, ILogicalDeviceSwitchableReadonly, ILogicalDeviceLight, ILogicalDeviceWithStatus, ILogicalDeviceLatchingRelay, IRelayBasic
	{
		public bool IsSecurityLight => base.LogicalId.FunctionName.IsSecurityLight();

		public override bool IsMasterSwitchControllable
		{
			get
			{
				if (base.DeviceCapability.AllLightsGroupBehavior != 0)
				{
					return base.DeviceCapability.AllLightsGroupBehavior == AllLightsGroupBehaviorCapability.FeatureSupportedAndEnabled;
				}
				return !IsSecurityLight;
			}
		}

		public override SwitchUsage UsedFor => SwitchUsage.Light;

		public LogicalDeviceLightType1(ILogicalDeviceId logicalDeviceId, LogicalDeviceRelayCapabilityType1 capability, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, capability, service, isFunctionClassChangeable)
		{
		}

		public override void OnLogicalIdChanged()
		{
			NotifyPropertyChanged("IsSecurityLight");
			NotifyPropertyChanged("IsMasterSwitchControllable");
			base.OnLogicalIdChanged();
		}
	}
}
