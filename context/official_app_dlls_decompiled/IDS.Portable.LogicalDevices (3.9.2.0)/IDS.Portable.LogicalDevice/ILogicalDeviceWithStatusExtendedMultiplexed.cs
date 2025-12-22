using System;
using System.Collections.Generic;
using System.ComponentModel;
using DynamicData;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceWithStatusExtendedMultiplexed : ILogicalDeviceWithStatusExtended, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		IEnumerable<IDeviceDataPacketMutableExtended> DeviceStatusExtendedAllRaw { get; }
	}
	public interface ILogicalDeviceWithStatusExtendedMultiplexed<TDeviceStatusExtended, TExtendedByteKey> : ILogicalDeviceWithStatusExtendedMultiplexed, ILogicalDeviceWithStatusExtended, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged where TDeviceStatusExtended : IDeviceDataPacketMutableExtended where TExtendedByteKey : notnull
	{
		IReadOnlyDictionary<TExtendedByteKey, TDeviceStatusExtended> DeviceStatusExtendedDictionary { get; }

		IObservableCache<TDeviceStatusExtended, TExtendedByteKey> DeviceStatusExtendedObservableCache { get; }

		IEnumerable<TDeviceStatusExtended> DeviceStatusExtendedAll { get; }

		bool UpdateDeviceStatusExtended(TDeviceStatusExtended statusExtended, DateTime? timeUpdated = null);

		TExtendedByteKey ToExtendedByteKey(byte extendedByte);
	}
}
