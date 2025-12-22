using System;

namespace OneControl.Devices
{
	public interface IPowerMonitorBatteryCharge
	{
		float BatteryChargerVoltage { get; }

		TimeSpan BatteryChargerVoltageLastUpdated { get; }

		uint BatteryChargerVoltageUpdateCount { get; }

		float BatteryCurrent { get; }

		TimeSpan BatteryCurrentLastUpdated { get; }

		uint BatteryCurrentCount { get; }

		PowerFlow BatteryCurrentFlow { get; }

		byte BatteryStateOfCharge { get; }

		TimeSpan BatteryStateOfChargeLastUpdated { get; }

		uint BatteryStateOfChargeUpdateCount { get; }

		TimeSpan BatteryMinutesRemaining { get; }

		TimeSpan BatteryMinutesRemainingLastUpdated { get; }

		uint BatteryMinutesRemainingUpdateCount { get; }
	}
}
