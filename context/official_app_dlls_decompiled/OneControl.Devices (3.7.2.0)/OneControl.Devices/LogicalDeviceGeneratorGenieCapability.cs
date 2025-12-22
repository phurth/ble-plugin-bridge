using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceGeneratorGenieCapability : LogicalDeviceCapability, ILogicalDeviceGeneratorGenieCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		private GeneratorGenieCapabilityFlag CapabilityFlag => (GeneratorGenieCapabilityFlag)RawValue;

		public bool IsAutoStartOnTempDifferentalSupported => CapabilityFlag.HasFlag(GeneratorGenieCapabilityFlag.SupportsAutoStartOnTempDifferental);

		public GeneratorType GeneratorType
		{
			get
			{
				if (!CapabilityFlag.HasFlag(GeneratorGenieCapabilityFlag.CumminsOnanGeneratorDetected))
				{
					return GeneratorType.Generic;
				}
				return GeneratorType.CumminsOnan;
			}
		}

		public LogicalDeviceGeneratorGenieCapability(byte? rawCapability)
		{
			UpdateDeviceCapability(rawCapability);
		}

		public LogicalDeviceGeneratorGenieCapability(ClimateZoneCapabilityFlag capabilityFlags)
		{
			UpdateDeviceCapability((byte)capabilityFlags);
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("IsAutoStartOnTempDifferentalSupported");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
