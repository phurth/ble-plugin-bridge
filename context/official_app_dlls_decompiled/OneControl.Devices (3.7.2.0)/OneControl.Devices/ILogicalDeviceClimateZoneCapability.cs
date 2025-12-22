using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceClimateZoneCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		bool HasAirConditioner { get; }

		bool HasHeatPump { get; }

		bool IsHeatPump { get; }

		bool IsGasHeat { get; }

		bool IsElectricHeat { get; }

		bool IsMultiSpeedFan { get; }

		bool IsValid { get; }
	}
}
