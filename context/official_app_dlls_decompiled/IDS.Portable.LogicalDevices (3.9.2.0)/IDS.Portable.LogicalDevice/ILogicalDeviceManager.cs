using System;
using System.Collections.Generic;
using DynamicData;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.ObservableCollection;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceManager : ICommonDisposable, IDisposable, IContainerDataSource, IContainerDataSourceBase
	{
		ILogicalDeviceService DeviceService { get; }

		IEnumerable<ILogicalDevice> LogicalDevices { get; }

		IObservableCache<ILogicalDevice, string> LogicalDeviceObservableCache { get; }

		LogicalDeviceTagManager TagManager { get; }

		ILogicalDevice? FindLogicalDevice(ILogicalDeviceId logicalDeviceId);

		ILogicalDevice? FindLogicalDevice(DEVICE_ID deviceId, MAC address);

		ILogicalDevice? FindLogicalDevice(DEVICE_ID? deviceIdOptional, MAC address);

		ILogicalDevice? FindLogicalDevice(Func<ILogicalDevice, bool> matches);

		ILogicalDevice? FindLogicalDeviceIgnoringName(DEVICE_ID deviceId, MAC address);

		ILogicalDevice? FindLogicalDeviceMatchingPhysicalHardware(DEVICE_ID deviceId, MAC address);

		List<TLogicalDevice> FindLogicalDevices<TLogicalDevice>(Func<TLogicalDevice, bool>? filter) where TLogicalDevice : class, ILogicalDevice;

		TLogicalDevice? RegisterLogicalDevice<TLogicalDevice>(TLogicalDevice? logicalDevice, ILogicalDeviceSource deviceSource) where TLogicalDevice : class, ILogicalDevice;

		ILogicalDevice? AddLogicalDevice(ILogicalDeviceId logicalDeviceId, byte? rawCapability, ILogicalDeviceSource deviceSource, Func<ILogicalDevice, bool> isAttemptAutoRenameEnabled);

		void AddLogicalDevices(LogicalDeviceSnapshot snapshot, Func<LogicalDeviceSnapshotDevice, bool>? includeFilter = null, Func<ILogicalDevice, bool>? isAttemptAutoRenameEnabled = null);

		LogicalDeviceSnapshot TakeSnapshot(Func<ILogicalDevice, bool>? includeFilter = null);

		void RemoveAllLogicalDevices();

		void RemoveLogicalDevice(Func<ILogicalDevice, bool> filter);

		void ContainerDataSourceSync(bool batchRequest);

		void RegisterLogicalDeviceExFactory(LogicalDeviceExFactory factory);
	}
}
