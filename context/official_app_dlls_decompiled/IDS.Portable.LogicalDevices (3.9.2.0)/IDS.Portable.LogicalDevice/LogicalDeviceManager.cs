using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DynamicData;
using IDS.Core.Collections;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.ObservableCollection;
using IDS.Portable.LogicalDevice.LogicalDevice;

namespace IDS.Portable.LogicalDevice
{
	internal class LogicalDeviceManager : CommonDisposable, ILogicalDeviceManager, ICommonDisposable, IDisposable, IContainerDataSource, IContainerDataSourceBase, ILogicalDeviceManagerIdsInternal
	{
		private const string LogTag = "LogicalDeviceManager";

		private readonly object _lock = new object();

		private const string TagCustomDataKey = "devicemanager.Tag";

		private readonly ISourceCache<ILogicalDevice, string> _logicalDeviceSourceCache;

		private readonly ConcurrentHashSet<LogicalDeviceExFactory> _logicalDeviceExFactoryList = new ConcurrentHashSet<LogicalDeviceExFactory>();

		public const int MaxCanDevices = 256;

		private readonly ILogicalDevice?[] _logicalDevicesLastKnownCanAddressMap = new ILogicalDevice[256];

		protected const int NotifyEventBatchDelayMs = 250;

		protected const int NotifyEventBatchMaxDelayMs = 1000;

		private Watchdog? _notifyEventBatchWatchdog;

		public object AddRemoveLock => _lock;

		public ILogicalDeviceService DeviceService { get; }

		public LogicalDeviceTagManager TagManager { get; }

		public IObservableCache<ILogicalDevice, string> LogicalDeviceObservableCache { get; }

		public IEnumerable<ILogicalDevice> LogicalDevices => LogicalDeviceObservableCache.Items;

		private Watchdog NotifyEventBatchWatchdog => _notifyEventBatchWatchdog ?? (_notifyEventBatchWatchdog = new Watchdog(250, 1000, delegate
		{
			this.ContainerDataSourceNotifyEvent?.Invoke(this, ContainerDataSourceNotifyRefresh.Default);
		}, autoStartOnFirstPet: true));

		public event ContainerDataSourceNotifyEventHandler? ContainerDataSourceNotifyEvent;

		public LogicalDeviceManager(ILogicalDeviceService deviceService)
		{
			_logicalDeviceSourceCache = new SourceCache<ILogicalDevice, string>((ILogicalDevice ld) => ld.ImmutableUniqueId);
			LogicalDeviceObservableCache = _logicalDeviceSourceCache.AsObservableCache();
			DeviceService = deviceService ?? throw new ArgumentNullException("deviceService");
			TagManager = new LogicalDeviceTagManager("devicemanager.Tag");
		}

		private ILogicalDevice? MakeLogicalDevice(ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			try
			{
				if (DeviceService == null)
				{
					TaggedLog.Debug("LogicalDeviceManager", "MakeLogicalDevice: Unable to create logical device as DeviceService isn't available!");
					return null;
				}
				if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid || logicalDeviceId.ProductMacAddress == null)
				{
					TaggedLog.Debug("LogicalDeviceManager", $"MakeLogicalDevice: Unable to create logical device as ILogicalDeviceId {logicalDeviceId} isn't valid");
					return null;
				}
				foreach (ILogicalDeviceFactory logicalDeviceFactory in DeviceService.LogicalDeviceFactoryList)
				{
					ILogicalDevice logicalDevice = logicalDeviceFactory.MakeLogicalDevice(DeviceService, logicalDeviceId, rawCapability);
					if (logicalDevice != null)
					{
						return logicalDevice;
					}
				}
			}
			catch (Exception arg)
			{
				TaggedLog.Error("LogicalDeviceManager", $"MakeLogicalDevice: Exception {arg}");
			}
			TaggedLog.Error("LogicalDeviceManager", $"MakeLogicalDevice: Error, Unable to create logical device for {logicalDeviceId}");
			return null;
		}

		public static bool FilterIncludeAll<TDataModel>(TDataModel dataModel)
		{
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ILogicalDevice? FindLogicalDevice(Func<ILogicalDevice, bool> matches)
		{
			Func<ILogicalDevice, bool> matches2 = matches;
			if (matches2 == null)
			{
				matches2 = FilterIncludeAll;
			}
			return Enumerable.FirstOrDefault(_logicalDeviceSourceCache.Items, (ILogicalDevice foundLogicalDevice) => !foundLogicalDevice.IsDisposed && matches2(foundLogicalDevice));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public List<TLogicalDevice> FindLogicalDevices<TLogicalDevice>(Func<TLogicalDevice, bool>? filter) where TLogicalDevice : class, ILogicalDevice
		{
			Func<TLogicalDevice, bool> filter2 = filter;
			if (filter2 == null)
			{
				filter2 = FilterIncludeAll;
			}
			return Enumerable.ToList(Enumerable.Where(Enumerable.OfType<TLogicalDevice>(_logicalDeviceSourceCache.Items), (TLogicalDevice matchLogicalDevice) => !matchLogicalDevice.IsDisposed && filter2(matchLogicalDevice)));
		}

		public ILogicalDevice? FindLogicalDevice(ILogicalDeviceId logicalDeviceId)
		{
			ILogicalDeviceId logicalDeviceId2 = logicalDeviceId;
			return FindLogicalDevice((ILogicalDevice logicalDevice) => logicalDevice.LogicalId.Equals(logicalDeviceId2));
		}

		public ILogicalDevice? FindLogicalDevice(DEVICE_ID? deviceIdOptional, MAC address)
		{
			if (!deviceIdOptional.HasValue)
			{
				return null;
			}
			return FindLogicalDevice(deviceIdOptional.Value, address);
		}

		public ILogicalDevice? FindLogicalDevice(DEVICE_ID deviceId, MAC address)
		{
			MAC address2 = address;
			return FindLogicalDevice((ILogicalDevice logicalDevice) => logicalDevice.LogicalId.IsForDeviceId(deviceId, address2));
		}

		public ILogicalDevice? FindLogicalDeviceIgnoringName(DEVICE_ID deviceId, MAC address)
		{
			MAC address2 = address;
			return FindLogicalDevice((ILogicalDevice logicalDevice) => logicalDevice.LogicalId.IsForDeviceIdIgnoringName(deviceId, address2, ignoreFunctionClass: false));
		}

		public ILogicalDevice? FindLogicalDeviceMatchingPhysicalHardware(DEVICE_ID deviceId, MAC address)
		{
			MAC address2 = address;
			return FindLogicalDevice((ILogicalDevice logicalDevice) => logicalDevice.LogicalId.IsMatchingPhysicalHardware(deviceId.ProductID, deviceId.DeviceType, deviceId.DeviceInstance, address2));
		}

		public ILogicalDevice? FindLogicalDevice(IDevice physicalDevice)
		{
			if (physicalDevice == null || physicalDevice.Address == ADDRESS.INVALID)
			{
				return null;
			}
			lock (_lock)
			{
				ILogicalDevice logicalDevice = _logicalDevicesLastKnownCanAddressMap[(byte)physicalDevice.Address];
				if (logicalDevice != null && logicalDevice.IsDisposed)
				{
					logicalDevice = null;
					_logicalDevicesLastKnownCanAddressMap[(byte)physicalDevice.Address] = null;
				}
				if (logicalDevice == null)
				{
					logicalDevice = FindLogicalDevice(physicalDevice.GetDeviceID(), physicalDevice.MAC);
					_logicalDevicesLastKnownCanAddressMap[(byte)physicalDevice.Address] = logicalDevice;
					return logicalDevice;
				}
				if (logicalDevice.LogicalId.IsForDeviceId(physicalDevice.GetDeviceID(), physicalDevice.MAC))
				{
					return logicalDevice;
				}
				_logicalDevicesLastKnownCanAddressMap[(byte)physicalDevice.Address] = null;
			}
			return null;
		}

		public TLogicalDevice? RegisterLogicalDevice<TLogicalDevice>(TLogicalDevice? logicalDevice, ILogicalDeviceSource deviceSource) where TLogicalDevice : class, ILogicalDevice
		{
			if (deviceSource == null)
			{
				TaggedLog.Debug("LogicalDeviceManager", $"Unable to register logical device because DeviceSource is null {logicalDevice}");
				return null;
			}
			return RegisterLogicalDeviceInternal(logicalDevice, deviceSource);
		}

		private TLogicalDevice? RegisterLogicalDeviceInternal<TLogicalDevice>(TLogicalDevice? logicalDevice, ILogicalDeviceSource? deviceSource) where TLogicalDevice : class, ILogicalDevice
		{
			lock (_lock)
			{
				ILogicalDeviceService deviceService = DeviceService;
				if (logicalDevice == null || deviceService.IsDisposed)
				{
					return null;
				}
				if (deviceSource != null)
				{
					logicalDevice!.AddDeviceSource(deviceSource);
					TagManager.AddTags(deviceSource!.MakeDeviceSourceTags(logicalDevice), logicalDevice);
				}
				if (deviceService.Options.HasFlag(LogicalDeviceServiceOptions.AutoFavoriteAccessoryDevices) && logicalDevice is IAccessoryDevice && !TagManager.ContainsTag(LogicalDeviceTagFavorite.DefaultFavoriteTag, logicalDevice))
				{
					TagManager.AddTag(LogicalDeviceTagFavorite.DefaultFavoriteTag, logicalDevice);
				}
				if (Enumerable.Contains(_logicalDeviceSourceCache.Items, logicalDevice))
				{
					TaggedLog.Information("LogicalDeviceManager", $"RegisterLogicalDevice: Registered logical device {logicalDevice} tags={TagManager.DebugTagsAsString(logicalDevice)} (Already Registered)");
					return logicalDevice;
				}
				PRODUCT_ID productId = logicalDevice!.LogicalId.ProductId;
				MAC productMacAddress = logicalDevice!.LogicalId.ProductMacAddress;
				if (productId != PRODUCT_ID.UNKNOWN && productMacAddress != null)
				{
					deviceService.ProductManager?.AddProduct(productId, productMacAddress);
				}
				_logicalDeviceSourceCache.AddOrUpdate(logicalDevice);
				TaggedLog.Information("LogicalDeviceManager", $"RegisterLogicalDevice: Registered logical device {logicalDevice} tags={TagManager.DebugTagsAsString(logicalDevice)}");
				ApplyAllExtensionFactories(logicalDevice);
				if (logicalDevice!.DeviceService?.GetPrimaryDeviceSourceDirect(logicalDevice) is ILogicalDeviceSourceDirectConnectionIdsCan logicalDeviceSourceDirectConnectionIdsCan)
				{
					logicalDeviceSourceDirectConnectionIdsCan.UpdateLogicalDeviceOnlineStatus(logicalDevice);
					return logicalDevice;
				}
				return logicalDevice;
			}
		}

		public ILogicalDevice? AddLogicalDevice(ILogicalDeviceId logicalDeviceId, byte? rawCapability, ILogicalDeviceSource deviceSource, Func<ILogicalDevice, bool> isAttemptAutoRenameEnabled)
		{
			return AddLogicalDeviceInternal(logicalDeviceId, rawCapability, deviceSource, isAttemptAutoRenameEnabled).logicalDevice;
		}

		public (ILogicalDevice? logicalDevice, bool isNew) AddLogicalDeviceInternal(ILogicalDeviceId logicalDeviceId, byte? rawCapability, ILogicalDeviceSource? deviceSource, Func<ILogicalDevice, bool> isAttemptAutoRenameEnabled)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid || logicalDeviceId.ProductMacAddress == null)
			{
				TaggedLog.Debug("LogicalDeviceManager", $"AddLogicalDevice: Unable to add device as deviceId {logicalDeviceId} is invalid!");
				return (null, false);
			}
			lock (_lock)
			{
				ILogicalDevice logicalDevice = null;
				logicalDevice = FindLogicalDevice(logicalDeviceId);
				if (logicalDevice == null && isAttemptAutoRenameEnabled != null && logicalDeviceId.ProductMacAddress != null)
				{
					DEVICE_ID deviceId = logicalDeviceId.MakeDeviceId(rawCapability, 0);
					ILogicalDevice logicalDevice2 = FindLogicalDeviceMatchingPhysicalHardware(deviceId, logicalDeviceId.ProductMacAddress);
					if (logicalDevice2 != null && isAttemptAutoRenameEnabled(logicalDevice2) && logicalDevice2.Rename(deviceId.FunctionName, deviceId.FunctionInstance))
					{
						TaggedLog.Debug("LogicalDeviceManager", $"renamed existing logical device to {logicalDevice2}");
						logicalDevice = logicalDevice2;
					}
				}
				if (logicalDevice != null)
				{
					if (deviceSource == null)
					{
						TaggedLog.Debug("LogicalDeviceManager", $"AddLogicalDevice: existing logical device found {logicalDevice} tags not supplied.");
					}
					else
					{
						TagManager.AddTags(deviceSource!.MakeDeviceSourceTags(logicalDevice), logicalDevice);
						logicalDevice.AddDeviceSource(deviceSource);
						TaggedLog.Debug("LogicalDeviceManager", $"AddLogicalDevice: existing logical device found {logicalDevice} tags={TagManager.DebugTagsAsString(logicalDevice)}");
					}
					if (logicalDevice.DeviceService?.GetPrimaryDeviceSourceDirect(logicalDevice) is ILogicalDeviceSourceDirectConnectionIdsCan logicalDeviceSourceDirectConnectionIdsCan)
					{
						logicalDeviceSourceDirectConnectionIdsCan.UpdateLogicalDeviceOnlineStatus(logicalDevice);
					}
					return (logicalDevice, false);
				}
				logicalDevice = MakeLogicalDevice(logicalDeviceId, rawCapability);
				if (logicalDevice == null)
				{
					TaggedLog.Error("LogicalDeviceManager", $"AddLogicalDevice: ERROR, Unable to add logical device for {logicalDeviceId} with MAC {logicalDeviceId.ProductMacAddress}");
					return (null, false);
				}
				RegisterLogicalDeviceInternal(logicalDevice, deviceSource);
				return (logicalDevice, true);
			}
		}

		public void RemoveAllLogicalDevices()
		{
			lock (_lock)
			{
				TaggedLog.Debug("LogicalDeviceManager", "LogicalDeviceManager: REMOVING ALL LOGICAL DEVICES");
				foreach (ILogicalDevice item in _logicalDeviceSourceCache.Items)
				{
					item.TryDispose();
				}
				_logicalDeviceSourceCache.Clear();
				for (int i = 0; i < _logicalDevicesLastKnownCanAddressMap.Length; i++)
				{
					_logicalDevicesLastKnownCanAddressMap[i] = null;
				}
			}
			MainThread.RequestMainThreadAction(delegate
			{
				ContainerDataSourceSync(batchRequest: false);
			});
		}

		public void RemoveLogicalDevice(Func<ILogicalDevice, bool> filter)
		{
			if (filter == null)
			{
				RemoveAllLogicalDevices();
				return;
			}
			lock (_lock)
			{
				List<ILogicalDevice> list = new List<ILogicalDevice>();
				foreach (ILogicalDevice item in _logicalDeviceSourceCache.Items)
				{
					try
					{
						if (filter(item))
						{
							list.Add(item);
						}
					}
					catch
					{
					}
				}
				foreach (ILogicalDevice item2 in list)
				{
					TaggedLog.Debug("LogicalDeviceManager", $"LogicalDeviceManager: REMOVING LOGICAL DEVICES {item2}");
					_logicalDeviceSourceCache.Remove(item2);
					item2.TryDispose();
				}
				for (int i = 0; i < 256; i++)
				{
					ILogicalDevice? obj2 = _logicalDevicesLastKnownCanAddressMap[i];
					if (obj2 != null && obj2!.IsDisposed)
					{
						_logicalDevicesLastKnownCanAddressMap[i] = null;
					}
				}
			}
			MainThread.RequestMainThreadAction(delegate
			{
				ContainerDataSourceSync(batchRequest: false);
			});
		}

		public void AddLogicalDevices(LogicalDeviceSnapshot snapshot, Func<LogicalDeviceSnapshotDevice, bool>? includeFilter = null, Func<ILogicalDevice, bool>? isAttemptAutoRenameEnabled = null)
		{
			if (snapshot == null)
			{
				return;
			}
			lock (_lock)
			{
				foreach (LogicalDeviceSnapshotDevice device in snapshot.Devices)
				{
					if (device == null || (includeFilter != null && !includeFilter!(device)))
					{
						continue;
					}
					IReadOnlyDictionary<string, LogicalDeviceSnapshotDeviceTag> deviceTags = device.DeviceTags;
					List<ILogicalDeviceTag> list = new List<ILogicalDeviceTag>((deviceTags?.TryGetValue(TagManager.TagKey))?.Tags ?? new List<ILogicalDeviceSnapshotTag>());
					if (isAttemptAutoRenameEnabled == null)
					{
						isAttemptAutoRenameEnabled = (ILogicalDevice ld) => true;
					}
					ILogicalDevice item = AddLogicalDeviceInternal(device.LogicalId, device.DeviceCapabilityRawValue, null, isAttemptAutoRenameEnabled).logicalDevice;
					if (item == null)
					{
						continue;
					}
					if (Enumerable.Any(list))
					{
						TagManager.AddTags(list, item);
						TaggedLog.Debug("LogicalDeviceManager", $"AddLogicalDevice: Added tags to {item} tags={TagManager.DebugTagsAsString(item)}");
					}
					if (deviceTags != null)
					{
						foreach (string key in deviceTags.Keys)
						{
							if (!string.Equals(key, TagManager.TagKey))
							{
								deviceTags.TryGetValue(key)?.AddTagsToLogicalDevice(item, key);
							}
						}
					}
					item.CustomSnapshotDataUpdate(device.CustomSnapshotData);
					if (device.StatusData != null && device.StatusData!.Length != 0 && item.ActiveConnection == LogicalDeviceActiveConnection.Offline && item is ILogicalDeviceWithStatus logicalDeviceWithStatus)
					{
						logicalDeviceWithStatus.UpdateDeviceStatusInternal(device.StatusData, (uint)device.StatusData!.Length, device.LastUpdatedTimestamp ?? DateTime.MinValue);
					}
					ILogicalDeviceWithStatusAlerts logicalDeviceWithStatusAlerts = item as ILogicalDeviceWithStatusAlerts;
					if (logicalDeviceWithStatusAlerts != null)
					{
						IReadOnlyDictionary<string, LogicalDeviceAlert>? alerts = device.Alerts;
						if (alerts != null)
						{
							Enumerable.ToList(alerts!.Values).ForEach(delegate(LogicalDeviceAlert alert)
							{
								logicalDeviceWithStatusAlerts.UpdateAlert(alert.Name, alert.IsActive, alert.Count);
							});
						}
					}
					item.SnapshotLoaded(snapshot);
				}
			}
		}

		public LogicalDeviceSnapshot TakeSnapshot(Func<ILogicalDevice, bool>? includeFilter = null)
		{
			return new LogicalDeviceSnapshot(FindLogicalDevices(includeFilter));
		}

		public void ContainerDataSourceSync(bool batchRequest)
		{
			if (batchRequest)
			{
				NotifyEventBatchWatchdog?.TryPet(autoReset: true);
			}
			else
			{
				ContainerDataSourceNotify(this, ContainerDataSourceNotifyRefresh.Default);
			}
		}

		public void ContainerDataSourceNotify(object sender, EventArgs args)
		{
			this.ContainerDataSourceNotifyEvent?.Invoke(sender, args);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public List<TDataModel> FindContainerDataMatchingFilter<TDataModel>(Func<TDataModel, bool> filter)
		{
			Func<TDataModel, bool> filter2 = filter;
			return Enumerable.ToList(Enumerable.Where(Enumerable.OfType<TDataModel>(_logicalDeviceSourceCache.Items), (TDataModel matchLogicalDevice) => (!((object)matchLogicalDevice is ILogicalDevice logicalDevice) || !logicalDevice.IsDisposed) && (filter2?.Invoke(matchLogicalDevice) ?? true)));
		}

		public void RegisterLogicalDeviceExFactory(LogicalDeviceExFactory factory)
		{
			lock (_lock)
			{
				_logicalDeviceExFactoryList.Add(factory);
				foreach (ILogicalDevice item in _logicalDeviceSourceCache.Items)
				{
					ApplyExtensionFactory(factory, item);
				}
			}
		}

		private void ApplyExtensionFactory(LogicalDeviceExFactory extensionFactory, ILogicalDevice logicalDevice, bool replaceExisting = false)
		{
			try
			{
				ILogicalDeviceEx logicalDeviceEx = extensionFactory(logicalDevice);
				if (logicalDeviceEx != null)
				{
					logicalDevice.AddLogicalDeviceEx(logicalDeviceEx, replaceExisting);
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceManager", $"MakeLogicalDeviceForPhysicalDevice: Unexpected exception from Extension Factory for {logicalDevice}: {ex.Message}");
			}
		}

		private void ApplyAllExtensionFactories(ILogicalDevice logicalDevice, bool replaceExisting = false)
		{
			lock (_lock)
			{
				foreach (LogicalDeviceExFactory logicalDeviceExFactory in _logicalDeviceExFactoryList)
				{
					ApplyExtensionFactory(logicalDeviceExFactory, logicalDevice, replaceExisting);
				}
			}
		}

		public override void Dispose(bool disposing)
		{
			this.ContainerDataSourceNotifyEvent = null;
			lock (_lock)
			{
				RemoveAllLogicalDevices();
			}
		}
	}
}
