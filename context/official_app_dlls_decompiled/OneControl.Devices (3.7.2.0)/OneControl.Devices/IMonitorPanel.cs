using System.Collections.Generic;
using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IMonitorPanel : IDevicesCommon, INotifyPropertyChanged
	{
		bool IsDeviceConfigurationDataValid { get; }

		bool AreDeviceDefinitionsValid { get; }

		ILogicalDevicePidULong AvailableMomentarySwitchesPid { get; }

		ILogicalDevicePidULong AvailableLatchingSwitchesPid { get; }

		ILogicalDevicePidULong AvailableSupplyTankIndicatorsPid { get; }

		ILogicalDevicePidULong AvailableWasteTankIndicatorsPid { get; }

		IReadOnlyDictionary<Pid, ILogicalDevicePidMonitorPanelDeviceId> PidDeviceIdDict { get; }
	}
}
