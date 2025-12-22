using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceWithStatusExtended : ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		IDeviceDataPacketMutableExtended RawDeviceStatusExtended { get; }

		event LogicalDeviceChangedEventHandler DeviceStatusExtendedChanged;

		Dictionary<byte, byte[]> CopyRawDeviceStatusExtendedAsDictionary();

		bool UpdateDeviceStatusExtended(IReadOnlyDictionary<byte, byte[]> statusData, Dictionary<byte, DateTime>? timeUpdatedByExtendedData, bool updateOnlyIfNewer);

		bool UpdateDeviceStatusExtended(IReadOnlyList<byte> statusData, uint dataLength, byte extendedByte, DateTime? timeUpdated = null);

		Task WaitForDeviceStatusExtendedToHaveDataAsync(int timeout, CancellationToken cancelToken);
	}
	public interface ILogicalDeviceWithStatusExtended<TDeviceStatusExtended> : ILogicalDeviceWithStatusExtended, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged where TDeviceStatusExtended : IDeviceDataPacketMutableExtended
	{
		TDeviceStatusExtended DeviceStatusExtended { get; }

		bool UpdateDeviceStatusExtended(TDeviceStatusExtended extendedStatus, DateTime? timeUpdated = null);
	}
}
