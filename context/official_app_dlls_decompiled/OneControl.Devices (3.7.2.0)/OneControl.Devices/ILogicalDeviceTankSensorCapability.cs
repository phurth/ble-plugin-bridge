using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceTankSensorCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		SensorPrecisionType SensorPrecisionType { get; }

		bool AreTankAlertsSupported { get; }

		bool IsBatteryLevelSupported { get; }

		bool IsTankCapacitySupported { get; }

		bool IsTankHeightOrientationSupported { get; }

		float SensorPrecision { get; }
	}
}
