using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IHourMeter : IDevicesCommon, INotifyPropertyChanged
	{
		bool Error { get; }

		bool MaintenancePastDue { get; }

		bool MaintenanceDue { get; }

		bool Running { get; }

		ulong OperatingSeconds { get; }

		ILogicalDevicePidTimeSpan HourMeterLastMaintenanceTimePid { get; }

		ILogicalDevicePidTimeSpan HourMeterMaintenancePeriodSecPid { get; }
	}
}
