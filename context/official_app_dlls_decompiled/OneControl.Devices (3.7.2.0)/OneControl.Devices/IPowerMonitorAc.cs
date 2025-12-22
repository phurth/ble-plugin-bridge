using System;

namespace OneControl.Devices
{
	public interface IPowerMonitorAc : IPowerMonitor
	{
		float AcVoltage { get; }

		TimeSpan AcVoltageLastUpdated { get; }

		uint AcVoltageUpdateCount { get; }

		float AcCurrent { get; }

		TimeSpan AcCurrentLastUpdated { get; }

		uint AcCurrentUpdateCount { get; }
	}
}
