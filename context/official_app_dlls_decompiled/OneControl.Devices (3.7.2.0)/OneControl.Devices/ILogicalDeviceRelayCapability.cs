using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		bool IsSoftwareConfigurableFuseSupported { get; }

		bool IsCoarsePositionSupported { get; }

		bool AreAutoCommandsSupported { get; }

		bool IsFinePositionSupported { get; }

		bool IsHomingSupported { get; }

		bool IsAwningSensorSupported { get; }

		PhysicalSwitchTypeCapability PhysicalSwitchType { get; }

		AllLightsGroupBehaviorCapability AllLightsGroupBehavior { get; }
	}
}
