namespace OneControl.Devices
{
	public interface IReadOnlyLogicalDeviceHourMeterStatus
	{
		bool Running { get; }

		bool MaintenanceDue { get; }

		bool MaintenancePastDue { get; }

		bool Stopping { get; }

		bool Starting { get; }

		bool Error { get; }

		bool IsHourMeterValid { get; }

		uint OperatingSeconds { get; }
	}
}
