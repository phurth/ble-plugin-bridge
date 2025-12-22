using System;
using System.ComponentModel;

namespace IDS.Portable.LogicalDevice.LogicalDeviceSource.ConnectionFailure
{
	public interface IConnectionFailure : INotifyPropertyChanged
	{
		DateTime? LastDateTime { get; }

		Exception? LastException { get; }

		Exception? ActiveException { get; }
	}
}
