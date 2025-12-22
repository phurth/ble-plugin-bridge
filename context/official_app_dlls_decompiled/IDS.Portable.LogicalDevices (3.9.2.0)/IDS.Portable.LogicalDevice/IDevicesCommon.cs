using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public interface IDevicesCommon : INotifyPropertyChanged
	{
		string DeviceName { get; }

		string DeviceNameShort { get; }

		string DeviceNameShortAbbreviated { get; }

		bool ActiveSession { get; }

		LogicalDeviceActiveConnection ActiveConnection { get; }
	}
}
