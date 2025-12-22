using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLevelerType4 : ILogicalDeviceLeveler, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, IDevicesActivation, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceWithStatus<LogicalDeviceLevelerStatusType4>, ILogicalDeviceWithStatus, ILogicalDeviceWithCapability<LogicalDeviceLevelerCapabilityType4>, ITextConsole
	{
		void UpdateDeviceConsoleText(List<string> text);

		ILogicalDeviceLightDimmable? GetAssociatedDimmableLight();

		Task<CommandResult> SendCommand(ILogicalDeviceLevelerCommandType4 command, CancellationToken callerCancelToken, bool ignoreScreenValidation = false);

		Task UpdateAutoStepsCollectionWithLatestDetails(int expectedStepsCount, BaseObservableCollection<(LogicalDeviceLevelerAutoStepType4 autoStep, int index)> collection, CancellationToken cancelToken);

		Task<(LogicalDeviceLevelerScreenType4 stepsScreen, int stepsCount, int stepsCompleted)> GetAutoStepsProgressAsync(CancellationToken cancelToken);

		Task<List<LogicalDeviceLevelerAutoStepType4>> GetAutoStepListDetailsAsync(int expectedStepsCount, CancellationToken cancelToken);
	}
}
