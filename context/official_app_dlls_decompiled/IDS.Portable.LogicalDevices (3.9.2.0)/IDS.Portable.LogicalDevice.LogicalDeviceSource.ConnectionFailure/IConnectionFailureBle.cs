using System.ComponentModel;

namespace IDS.Portable.LogicalDevice.LogicalDeviceSource.ConnectionFailure
{
	public interface IConnectionFailureBle<out TConnectionFailure> : IConnectionFailure, INotifyPropertyChanged
	{
		TConnectionFailure ActiveConnectionFailure { get; }

		TConnectionFailure LastConnectionFailure { get; }
	}
}
