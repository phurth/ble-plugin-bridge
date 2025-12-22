using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.TemperatureSensor
{
	public interface ILogicalDeviceTemperatureSensorCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		bool IsCoinCellBatterySupported { get; }

		bool IsHighTemperatureAlertSupported { get; }

		bool IsLowTemperatureAlertSupported { get; }

		bool IsLowBatteryAlertSupported { get; }

		bool IsTemperatureInRangeAlertSupported { get; }

		bool IsBatteryCapacityReportingSupported { get; }
	}
}
