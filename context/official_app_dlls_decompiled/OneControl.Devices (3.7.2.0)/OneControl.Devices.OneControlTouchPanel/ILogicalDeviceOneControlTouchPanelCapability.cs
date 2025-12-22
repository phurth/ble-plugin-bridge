using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.OneControlTouchPanel
{
	public interface ILogicalDeviceOneControlTouchPanelCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		bool IsHighResolutionTanksSupported { get; }

		bool HasStaticMacAddress { get; }
	}
}
