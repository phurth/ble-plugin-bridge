using System;

namespace OneControl.Devices
{
	public interface IPowerMonitorDcOutput : IPowerMonitor
	{
		float DcVoltageOutput { get; }

		TimeSpan DcVoltageOutputLastUpdated { get; }

		uint DcVoltageOutputUpdateCount { get; }

		float DcCurrentOutput { get; }

		TimeSpan DcCurrentOutputLastUpdated { get; }

		uint DcCurrentOutputUpdateCount { get; }
	}
}
