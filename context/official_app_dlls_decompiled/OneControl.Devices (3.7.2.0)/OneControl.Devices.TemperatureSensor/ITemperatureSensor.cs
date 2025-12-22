using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.LogicalDevice;

namespace OneControl.Devices.TemperatureSensor
{
	public interface ITemperatureSensor : IAccessoryDevice
	{
		int SensorBatteryChargePercent { get; }

		float TemperatureCelsius { get; }

		bool IsTemperatureValid { get; }

		float SensorBatteryVoltage { get; }

		bool IsSensorBatteryVoltageValid { get; }

		bool IsBatteryLow { get; }

		bool IsTemperatureHigh { get; }

		bool IsTemperatureLow { get; }

		bool IsTemperatureInRange { get; }

		int LowBatteryAlertCount { get; }

		int TemperatureHighAlertCount { get; }

		int TemperatureLowAlertCount { get; }

		int TemperatureInRangeAlertCount { get; }

		Task<float?> TryGetTemperatureHighAlertTriggerValueAsync(TemperatureScale scale, CancellationToken cancellationToken);

		Task TrySetTemperatureHighAlertTriggerValueAsync(float? value, TemperatureScale scale, CancellationToken cancellationToken);

		Task<float?> TryGetTemperatureLowAlertTriggerValueAsync(TemperatureScale scale, CancellationToken cancellationToken);

		Task TrySetTemperatureLowAlertTriggerValueAsync(float? value, TemperatureScale scale, CancellationToken cancellationToken);

		void SetTemperatureAlertTriggerCachedValues(float? lowValue, float? highValue, TemperatureScale scale);
	}
}
