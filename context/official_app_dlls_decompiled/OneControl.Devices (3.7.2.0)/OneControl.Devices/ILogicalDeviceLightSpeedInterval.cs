using System;
using System.ComponentModel;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLightSpeedInterval : ILogicalDeviceLight, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		bool SpeedIntervalEnabled { get; }

		LogicalDeviceLightSpeedInterval SpeedInterval { get; }

		Task<CommandResult> SetSpeedIntervalAsync(LogicalDeviceLightSpeedInterval speedInterval);
	}
}
