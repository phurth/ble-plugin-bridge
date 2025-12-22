using System;

namespace OneControl.Devices
{
	public interface IPowerMonitorSolar : IPowerMonitor
	{
		float SolarVoltage { get; }

		TimeSpan SolarVoltageLastUpdated { get; }

		uint SolarVoltageUpdateCount { get; }

		float SolarCurrent { get; }

		TimeSpan SolarCurrentLastUpdated { get; }

		uint SolarCurrentUpdateCount { get; }
	}
}
