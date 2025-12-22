using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace OneControl.Devices
{
	public interface ITankSensor
	{
		byte Level { get; }

		byte? BatteryLevel { get; }

		byte? MeasurementQuality { get; }

		float? XAcceleration { get; }

		float? YAcceleration { get; }

		TankSensorType SensorType { get; }

		TankHoldingType HoldingType { get; }

		bool IsEmbeddedTank { get; }

		bool IsTankLevelOutsideThreshold { get; }

		[Obsolete("Use the SensorPrecisionType property instead")]
		bool IsHighPrecisionTank { get; }

		SensorPrecisionType SensorPrecisionType { get; }

		bool IsLegacyTank { get; }

		bool IsSetAlertTankLevelThresholdSupported { get; }

		bool IsTankCapacitySupported { get; }

		bool IsTankHeightOrientationSupported { get; }

		TankSensorCapacity? TankCapacity { get; set; }

		AsyncValueCachedState TankCapacityState { get; }

		TankSensorAlertThreshold AlertThreshold { get; set; }

		AsyncValueCachedState AlertThresholdState { get; }

		Task<LogicalDeviceTankSensor.TankOrientationData?> GetTankOrientationDataAsync(CancellationToken cancellationToken = default(CancellationToken));

		Task SetVerticalTankConfigurationAsync(int heightInMillimeters, CancellationToken cancellationToken = default(CancellationToken));

		Task SetHorizontalTankConfigurationAsync(int heightInMillimeters, CancellationToken cancellationToken = default(CancellationToken));

		Task<TankSensorCapacity?> GetTankCapacityAsync(CancellationToken cancellationToken);

		Task SetTankCapacity(TankSensorCapacity value, CancellationToken cancellationToken);

		Task<TankSensorAlertThreshold> GetAlertTankLevelThresholdAsync(CancellationToken cancellationToken);

		Task SetAlertTankLevelThreshold(TankSensorAlertThreshold alertThreshold, CancellationToken cancellationToken);
	}
}
