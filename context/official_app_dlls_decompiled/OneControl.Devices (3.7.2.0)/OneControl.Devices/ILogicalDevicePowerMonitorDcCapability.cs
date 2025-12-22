using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDevicePowerMonitorDcCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		bool IsBatteryCapacityAmpHoursSupported { get; }
	}
}
