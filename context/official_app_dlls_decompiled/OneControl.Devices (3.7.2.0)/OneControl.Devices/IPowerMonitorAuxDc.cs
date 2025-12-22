using System;

namespace OneControl.Devices
{
	public interface IPowerMonitorAuxDc : IPowerMonitor
	{
		float AuxDcVoltage { get; }

		TimeSpan AuxDcVoltageLastUpdated { get; }

		uint AuxDcVoltageUpdateCount { get; }

		float AuxDcCurrent { get; }

		TimeSpan AuxDcCurrentLastUpdated { get; }

		uint AuxDcCurrentUpdateCount { get; }

		PowerFlow AuxDcPowerFlow { get; }
	}
}
