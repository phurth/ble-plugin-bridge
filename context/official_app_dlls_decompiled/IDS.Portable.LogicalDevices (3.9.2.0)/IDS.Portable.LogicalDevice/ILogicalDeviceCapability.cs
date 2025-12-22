using System.Collections.Generic;
using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceCapability : INotifyPropertyChanged
	{
		IEnumerable<LogicalDeviceCapabilitySerializable> ActiveCapabilities { get; }

		event DeviceCapabilityChangedEventHandler DeviceCapabilityChangedEvent;

		bool UpdateDeviceCapability(byte? rawDeviceCapability);

		byte GetRawValue();
	}
}
