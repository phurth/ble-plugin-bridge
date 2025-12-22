using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IChassisInfo : IDevicesCommon, INotifyPropertyChanged
	{
		ParkBrake ParkBreak { get; }

		IgnitionPowerSignal IgnitionPowerSignal { get; }
	}
}
