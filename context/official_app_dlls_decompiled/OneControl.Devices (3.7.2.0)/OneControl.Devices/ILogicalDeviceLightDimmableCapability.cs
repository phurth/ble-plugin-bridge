using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceLightDimmableCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		bool IsExtendedStatusSupported { get; }

		SimulatedOnOffStyleLightCapability SimulatedOnOffStyleLight { get; }

		bool RgbGangable { get; }

		PhysicalSwitchTypeCapability PhysicalSwitchType { get; }

		AllLightsGroupBehaviorCapability AllLightsGroupBehavior { get; }
	}
}
