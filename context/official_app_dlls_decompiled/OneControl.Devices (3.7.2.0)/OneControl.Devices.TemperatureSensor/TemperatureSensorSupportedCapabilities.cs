using System;

namespace OneControl.Devices.TemperatureSensor
{
	[Flags]
	public enum TemperatureSensorSupportedCapabilities : byte
	{
		None = 0,
		CoinCellBattery = 1,
		TemperatureHighAlert = 2,
		TemperatureLowAlert = 4,
		BatteryAlert = 8,
		TemperatureInRangeAlert = 0x10,
		BatteryCapacityReporting = 0x20,
		ValidFlags = 0x3F
	}
}
