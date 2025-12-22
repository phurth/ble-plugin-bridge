using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLightRgbCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		bool RgbUnGangable { get; }

		PhysicalSwitchTypeCapability PhysicalSwitchType { get; }

		AllLightsGroupBehaviorCapability AllLightsGroupBehavior { get; }
	}
}
