using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceWithStatus : ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		IDeviceDataPacketMutable RawDeviceStatus { get; }

		DateTime LastUpdatedTimestamp { get; }

		event LogicalDeviceChangedEventHandler? DeviceStatusChanged;

		bool UpdateDeviceStatus(IReadOnlyList<byte> statusData, uint dataLength);

		internal bool UpdateDeviceStatusInternal(IReadOnlyList<byte> statusData, uint dataLength, DateTime timeUpdated);

		void OnDeviceStatusChanged();

		Task WaitForDeviceStatusToHaveDataAsync(int timeout, CancellationToken cancelToken);
	}
	public interface ILogicalDeviceWithStatus<out TDeviceStatus> : ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged where TDeviceStatus : IDeviceDataPacketMutable
	{
		TDeviceStatus DeviceStatus { get; }
	}
}
