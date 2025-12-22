using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceGeneratorGenieCapability : ILogicalDeviceCapability, INotifyPropertyChanged
	{
		bool IsAutoStartOnTempDifferentalSupported { get; }

		GeneratorType GeneratorType { get; }
	}
}
