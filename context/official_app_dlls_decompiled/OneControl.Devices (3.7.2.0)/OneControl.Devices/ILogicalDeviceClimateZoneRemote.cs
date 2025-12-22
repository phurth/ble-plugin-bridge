using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;

namespace OneControl.Devices
{
	public interface ILogicalDeviceClimateZoneRemote : ILogicalDeviceClimateZone, IDevicesActivation, ILogicalDeviceWithStatus<LogicalDeviceClimateZoneStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceClimateZoneStatus>, ILogicalDeviceTemperatureMeasurementInside, ILogicalDeviceTemperatureMeasurement, ILogicalDeviceTemperatureMeasurementOutside, ILogicalDeviceCommandable<ClimateZoneHeatMode>, ILogicalDeviceCommandable, ICommandable<ClimateZoneHeatMode>, ILogicalDeviceCommandable<ClimateZoneFanMode>, ICommandable<ClimateZoneFanMode>, ILogicalDeviceCommandable<ClimateZoneHeatSource>, ICommandable<ClimateZoneHeatSource>, ILogicalDeviceRemote
	{
	}
}
