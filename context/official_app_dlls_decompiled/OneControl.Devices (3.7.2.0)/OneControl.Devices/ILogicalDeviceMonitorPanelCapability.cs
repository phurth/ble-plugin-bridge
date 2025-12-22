using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceMonitorPanelCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		bool HasBlePairingButton { get; }

		bool SupportsHighResolutionTanks { get; }
	}
}
