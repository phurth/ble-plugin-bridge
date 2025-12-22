using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceClimateZoneCapability : LogicalDeviceCapability, ILogicalDeviceClimateZoneCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		private ClimateZoneCapabilityFlag _capabilityFlag => (ClimateZoneCapabilityFlag)RawValue;

		public bool HasAirConditioner => _capabilityFlag.HasFlag(ClimateZoneCapabilityFlag.AirConditioner);

		public bool HasHeatPump
		{
			get
			{
				if (_capabilityFlag.HasFlag(ClimateZoneCapabilityFlag.HeatPump))
				{
					return HasAirConditioner;
				}
				return false;
			}
		}

		public bool IsHeatPump => _capabilityFlag.HasFlag(ClimateZoneCapabilityFlag.HeatPump);

		public bool IsGasHeat => _capabilityFlag.HasFlag(ClimateZoneCapabilityFlag.GasFurnace);

		public bool IsElectricHeat => false;

		public bool IsMultiSpeedFan => _capabilityFlag.HasFlag(ClimateZoneCapabilityFlag.MultiSpeedFan);

		public bool IsValid => _capabilityFlag != ClimateZoneCapabilityFlag.None;

		public LogicalDeviceClimateZoneCapability(byte? rawCapability)
		{
			UpdateDeviceCapability(rawCapability);
		}

		public LogicalDeviceClimateZoneCapability(ClimateZoneCapabilityFlag capabilityFlags)
		{
			UpdateDeviceCapability((byte)capabilityFlags);
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("HasAirConditioner");
			NotifyPropertyChanged("HasHeatPump");
			NotifyPropertyChanged("IsGasHeat");
			NotifyPropertyChanged("IsElectricHeat");
			NotifyPropertyChanged("IsMultiSpeedFan");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
