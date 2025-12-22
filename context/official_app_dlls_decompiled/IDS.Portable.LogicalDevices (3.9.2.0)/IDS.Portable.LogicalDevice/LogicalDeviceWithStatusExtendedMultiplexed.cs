using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.LogicalDevice
{
	public abstract class LogicalDeviceWithStatusExtendedMultiplexed<TDeviceStatus, TDeviceStatusExtended, TExtendedByteKey, TCapability> : LogicalDevice<TDeviceStatus, TCapability>, ILogicalDeviceWithStatusExtendedMultiplexed<TDeviceStatusExtended, TExtendedByteKey>, ILogicalDeviceWithStatusExtendedMultiplexed, ILogicalDeviceWithStatusExtended, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged where TDeviceStatus : IDeviceDataPacketMutable where TDeviceStatusExtended : IDeviceDataPacketMutableExtended where TExtendedByteKey : notnull where TCapability : ILogicalDeviceCapability
	{
		private const string LogTag = "LogicalDeviceWithStatusExtendedMultiplexed";

		private readonly TaskCompletionSource<bool> _tcsDeviceStatusExtendedHasData = new TaskCompletionSource<bool>();

		private readonly object _lock = new object();

		private readonly ConcurrentDictionary<TExtendedByteKey, TDeviceStatusExtended> _deviceStatusExtendedDictionary = new ConcurrentDictionary<TExtendedByteKey, TDeviceStatusExtended>();

		private readonly ISourceCache<TDeviceStatusExtended, TExtendedByteKey> _deviceStatusExtendedSourceCache;

		public IReadOnlyDictionary<TExtendedByteKey, TDeviceStatusExtended> DeviceStatusExtendedDictionary => _deviceStatusExtendedDictionary;

		public IObservableCache<TDeviceStatusExtended, TExtendedByteKey> DeviceStatusExtendedObservableCache { get; }

		public IEnumerable<TDeviceStatusExtended> DeviceStatusExtendedAll => DeviceStatusExtendedObservableCache.Items;

		public IEnumerable<IDeviceDataPacketMutableExtended> DeviceStatusExtendedAllRaw => Enumerable.Cast<IDeviceDataPacketMutableExtended>(DeviceStatusExtendedAll);

		public IDeviceDataPacketMutableExtended RawDeviceStatusExtended { get; }

		public event LogicalDeviceChangedEventHandler? DeviceStatusExtendedChanged;

		public virtual Dictionary<byte, byte[]> CopyRawDeviceStatusExtendedAsDictionary()
		{
			Dictionary<byte, byte[]> dictionary = new Dictionary<byte, byte[]>();
			foreach (IDeviceDataPacketMutableExtended item in DeviceStatusExtendedAllRaw)
			{
				dictionary[item.ExtendedByte] = item.CopyCurrentData();
			}
			return dictionary;
		}

		protected LogicalDeviceWithStatusExtendedMultiplexed(ILogicalDeviceId logicalDeviceId, TDeviceStatus status, TDeviceStatusExtended statusExtended, TCapability deviceCapability, ILogicalDeviceService deviceService, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, status, deviceCapability, deviceService, isFunctionClassChangeable)
		{
			_deviceStatusExtendedSourceCache = new SourceCache<TDeviceStatusExtended, TExtendedByteKey>((TDeviceStatusExtended ai) => ToExtendedByteKey(ai.ExtendedByte));
			DeviceStatusExtendedObservableCache = _deviceStatusExtendedSourceCache.AsObservableCache();
			RawDeviceStatusExtended = statusExtended;
			_deviceStatusExtendedSourceCache.AddOrUpdate(statusExtended);
		}

		public bool UpdateDeviceStatusExtended(TDeviceStatusExtended statusExtended, DateTime? timeUpdated = null)
		{
			return UpdateDeviceStatusExtended(statusExtended.Data, statusExtended.Size, statusExtended.ExtendedByte, timeUpdated);
		}

		public bool UpdateDeviceStatusExtended(IReadOnlyDictionary<byte, byte[]> statusData, Dictionary<byte, DateTime>? timeUpdatedByExtendedData, bool updateOnlyIfNewer)
		{
			bool flag = false;
			foreach (KeyValuePair<byte, byte[]> statusDatum in statusData)
			{
				if (statusDatum.Value != null && statusDatum.Value.Length != 0)
				{
					DateTime? dateTime = timeUpdatedByExtendedData?.TryGetValue(statusDatum.Key);
					TExtendedByteKey val = ToExtendedByteKey(statusDatum.Key);
					if (updateOnlyIfNewer && _deviceStatusExtendedDictionary.TryGetValue(val, out var val2) && val2.HasData && dateTime.HasValue && val2.LastUpdatedTimestamp > dateTime)
					{
						TaggedLog.Information("LogicalDeviceWithStatusExtendedMultiplexed", $"Device Status Extended data not applied for [{val}]/{statusDatum.Value.DebugDump()} because it's older then the current data in the buffer: {this}");
					}
					else
					{
						flag |= UpdateDeviceStatusExtended(statusDatum.Value, (uint)statusDatum.Value.Length, statusDatum.Key, dateTime);
					}
				}
			}
			return flag;
		}

		public bool UpdateDeviceStatusExtended(IReadOnlyList<byte> statusExtendedData, uint dataLength, byte extendedByte, DateTime? timeUpdated = null)
		{
			bool flag = false;
			TExtendedByteKey extendedByteTyped = ToExtendedByteKey(extendedByte);
			TDeviceStatusExtended toExtendedStatus;
			lock (_lock)
			{
				try
				{
					toExtendedStatus = _deviceStatusExtendedDictionary.GetOrAdd(extendedByteTyped, (TExtendedByteKey key) => MakeNewStatusExtendedFromMultiplexedData(extendedByteTyped));
				}
				catch (Exception ex)
				{
					TaggedLog.Error("LogicalDeviceWithStatusExtendedMultiplexed", $"{this} - Exception updating status extended {ex}: {ex.StackTrace}");
					return false;
				}
			}
			bool num = UpdateDeviceStatusExtended(statusExtendedData, dataLength, extendedByte, toExtendedStatus, timeUpdated) || flag;
			if (num)
			{
				_deviceStatusExtendedDictionary.AddOrUpdate(extendedByteTyped, toExtendedStatus, (TExtendedByteKey key, TDeviceStatusExtended oldValue) => toExtendedStatus);
				_deviceStatusExtendedSourceCache.AddOrUpdate(toExtendedStatus);
				NotifyPropertyChanged("RawDeviceStatusExtended");
				OnDeviceStatusExtendedChanged(toExtendedStatus, extendedByteTyped);
			}
			return num;
		}

		private bool UpdateDeviceStatusExtended(IReadOnlyList<byte> statusExtendedData, uint dataLength, byte extendedByte, TDeviceStatusExtended statusExtended, DateTime? timeUpdated = null)
		{
			bool flag = false;
			try
			{
				byte[] data = statusExtended.Data;
				byte extendedByte2 = statusExtended.ExtendedByte;
				flag = statusExtended.Update(statusExtendedData, dataLength, extendedByte, timeUpdated);
				if (flag)
				{
					DebugUpdateDeviceStatusExtendedChanged(data, statusExtendedData, dataLength, extendedByte2, extendedByte);
				}
				if (statusExtended.HasData)
				{
					_tcsDeviceStatusExtendedHasData.TrySetResult(true);
					return flag;
				}
				return flag;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceWithStatusExtendedMultiplexed", $"{this} - Exception updating status extended {ex}: {ex.StackTrace}");
				return flag;
			}
		}

		protected virtual void DebugUpdateDeviceStatusExtendedChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, byte oldExtendedByte, byte extendedByte, string optionalText = "")
		{
			TaggedLog.Debug("LogicalDeviceWithStatusExtendedMultiplexed", $"{this} - Status Extended changed from ({oldExtendedByte}):{oldStatusData.DebugDump(0, (int)dataLength)} to ({extendedByte}):{statusData.DebugDump(0, (int)dataLength)}{optionalText}");
		}

		protected virtual void OnDeviceStatusExtendedChanged(TDeviceStatusExtended dataChanged, TExtendedByteKey key)
		{
			this.DeviceStatusExtendedChanged?.Invoke(this);
		}

		public async Task WaitForDeviceStatusExtendedToHaveDataAsync(int timeout, CancellationToken cancelToken)
		{
			if (!DeviceStatus.HasData)
			{
				await _tcsDeviceStatusExtendedHasData.WaitAsync(cancelToken, timeout);
			}
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			this.DeviceStatusExtendedChanged = null;
		}

		public abstract TExtendedByteKey ToExtendedByteKey(byte extendedByte);

		protected abstract TDeviceStatusExtended MakeNewStatusExtendedFromMultiplexedData(TExtendedByteKey extendedByteKey);
	}
}
