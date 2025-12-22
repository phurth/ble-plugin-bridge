using System;
using System.ComponentModel;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;

namespace OneControl.Devices
{
	public interface ILogicalDeviceClimateZone : IDevicesActivation, ILogicalDeviceWithStatus<LogicalDeviceClimateZoneStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceClimateZoneStatus>, ILogicalDeviceTemperatureMeasurementInside, ILogicalDeviceTemperatureMeasurement, ILogicalDeviceTemperatureMeasurementOutside, ILogicalDeviceCommandable<ClimateZoneHeatMode>, ILogicalDeviceCommandable, ICommandable<ClimateZoneHeatMode>, ILogicalDeviceCommandable<ClimateZoneFanMode>, ICommandable<ClimateZoneFanMode>, ILogicalDeviceCommandable<ClimateZoneHeatSource>, ICommandable<ClimateZoneHeatSource>
	{
		bool IsRunningCommands { get; }

		ILogicalDeviceClimateZoneCapability DeviceCapability { get; }

		ClimateZoneHeatMode HeatMode { get; }

		ClimateZoneHeatSource HeatSource { get; }

		ClimateZoneFanMode FanMode { get; }

		Task<CommandResult> SendCommandAsync(ClimateZoneHeatMode heatMode, ClimateZoneHeatSource heatSource, ClimateZoneFanMode fanMode, byte lowTripTemperatureFahrenheit, byte highTripTemperatureFahrenheit, ClimateZoneCommandOptions commandOptions = ClimateZoneCommandOptions.AutoAdjustToSupportedConfiguration);
	}
}
