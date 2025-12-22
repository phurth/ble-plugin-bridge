using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceSourceDirectManager : ILogicalDeviceSourceDirectManager
	{
		private const string LogTag = "LogicalDeviceSourceDirectManager";

		private const int DeviceSourceInitialCapacity = 255;

		private IReadOnlyList<ILogicalDeviceSourceDirect> _deviceSources = new List<ILogicalDeviceSourceDirect>(255);

		private readonly ILogicalDeviceService _logicalDeviceService;

		public IEnumerable<ILogicalDeviceSourceDirect> DeviceSources => _deviceSources;

		public LogicalDeviceSourceDirectManager(ILogicalDeviceService logicalDeviceService)
		{
			_logicalDeviceService = logicalDeviceService;
		}

		public void SetDeviceSource(ILogicalDeviceSourceDirect? logicalDeviceSource)
		{
			if (logicalDeviceSource == null)
			{
				ClearDeviceSource();
			}
			else if (_deviceSources.Count != 1 || Enumerable.First(_deviceSources) != logicalDeviceSource)
			{
				List<ILogicalDeviceSourceDirect> deviceSourceList = new List<ILogicalDeviceSourceDirect> { logicalDeviceSource };
				SetDeviceSourceList(deviceSourceList);
			}
		}

		public void SetDeviceSourceList(List<ILogicalDeviceSourceDirect>? logicalDeviceSources)
		{
			if (logicalDeviceSources == null && _deviceSources.Count == 0)
			{
				return;
			}
			IReadOnlyList<ILogicalDeviceSourceDirect> readOnlyList = Enumerable.ToList((logicalDeviceSources ?? new List<ILogicalDeviceSourceDirect>()).RemoveDuplicates());
			IReadOnlyList<ILogicalDeviceSourceDirect> readOnlyList2 = new List<ILogicalDeviceSourceDirect>(_deviceSources);
			_deviceSources = readOnlyList;
			List<ILogicalDeviceSourceDirect> list = Enumerable.ToList(Enumerable.Except(readOnlyList2, readOnlyList));
			foreach (ILogicalDeviceSourceDirect item in list)
			{
				if (item is ILogicalDeviceSourceConnection logicalDeviceSourceConnection)
				{
					try
					{
						logicalDeviceSourceConnection.UpdateDeviceSourceReachabilityEvent -= DeviceSourceOnUpdateReachabilityEvent;
					}
					catch
					{
					}
				}
				TaggedLog.Information("LogicalDeviceSourceDirectManager", "Device Source Remove: " + item.GetType().Name + ":" + item.DeviceSourceToken);
			}
			List<ILogicalDeviceSourceDirect> list2 = Enumerable.ToList(Enumerable.Except(readOnlyList, readOnlyList2));
			foreach (ILogicalDeviceSourceDirect item2 in list2)
			{
				if (item2 is ILogicalDeviceSourceConnection logicalDeviceSourceConnection2)
				{
					try
					{
						logicalDeviceSourceConnection2.UpdateDeviceSourceReachabilityEvent += DeviceSourceOnUpdateReachabilityEvent;
					}
					catch
					{
					}
				}
				TaggedLog.Information("LogicalDeviceSourceDirectManager", "Device Source Added: " + item2.GetType().Name + ":" + item2.DeviceSourceToken);
			}
			List<ILogicalDeviceSourceDirect> list3 = new List<ILogicalDeviceSourceDirect>(list);
			list3.AddRange(list2);
			UpdatePrimaryLogicalDevices(list3);
		}

		public void ClearDeviceSource()
		{
			SetDeviceSourceList(null);
		}

		public List<TLogicalDeviceSource> FindDeviceSources<TLogicalDeviceSource>(Func<TLogicalDeviceSource, bool>? filter) where TLogicalDeviceSource : ILogicalDeviceSourceDirect
		{
			List<TLogicalDeviceSource> list = new List<TLogicalDeviceSource>();
			foreach (ILogicalDeviceSourceDirect deviceSource in _deviceSources)
			{
				if (deviceSource is TLogicalDeviceSource val && (filter == null || filter!(val)))
				{
					list.Add(val);
				}
			}
			return list;
		}

		public TLogicalDeviceSource? FindFirstDeviceSource<TLogicalDeviceSource>(Func<TLogicalDeviceSource, bool>? filter) where TLogicalDeviceSource : class, ILogicalDeviceSourceDirect
		{
			foreach (ILogicalDeviceSourceDirect deviceSource in _deviceSources)
			{
				if (deviceSource is TLogicalDeviceSource val && (filter == null || filter!(val)))
				{
					return val;
				}
			}
			return null;
		}

		public void ForeachDeviceSource<TLogicalDeviceSource>(Action<TLogicalDeviceSource> action, Func<TLogicalDeviceSource, bool>? filter) where TLogicalDeviceSource : class, ILogicalDeviceSourceDirect
		{
			foreach (ILogicalDeviceSourceDirect deviceSource in _deviceSources)
			{
				if (deviceSource is TLogicalDeviceSource val && (filter == null || filter!(val)))
				{
					try
					{
						action?.Invoke(val);
					}
					catch (Exception ex)
					{
						TaggedLog.Warning("LogicalDeviceSourceDirectManager", "ForeachDeviceSource action failed: " + ex.Message + " ");
					}
				}
			}
		}

		public void ForeachDeviceSource<TLogicalDeviceSource>(Action<TLogicalDeviceSource> action) where TLogicalDeviceSource : class, ILogicalDeviceSourceDirect
		{
			ForeachDeviceSource(action, null);
		}

		public TLogicalDeviceSource? GetPrimaryDeviceSource<TLogicalDeviceSource>(ILogicalDevice device) where TLogicalDeviceSource : class, ILogicalDeviceSourceDirect
		{
			TLogicalDeviceSource val = null;
			foreach (ILogicalDeviceSourceDirect deviceSource in _deviceSources)
			{
				if (!device.IsAssociatedWithDeviceSource(deviceSource) || !(deviceSource is TLogicalDeviceSource val2))
				{
					continue;
				}
				if (!(val2 is ILogicalDeviceSourceConnection logicalDeviceSourceConnection))
				{
					if (deviceSource.IsLogicalDeviceOnline(device))
					{
						return val2;
					}
					if (val == null || val is ILogicalDeviceSourceConnection)
					{
						val = val2;
					}
					continue;
				}
				switch (logicalDeviceSourceConnection.DeviceSourceReachability(device))
				{
				case LogicalDeviceReachability.Reachable:
					return val2;
				case LogicalDeviceReachability.Unreachable:
					if (val == null)
					{
						val = val2;
					}
					break;
				}
			}
			return val;
		}

		private void DeviceSourceOnUpdateReachabilityEvent(ILogicalDeviceSourceDirect deviceSource)
		{
			UpdatePrimaryLogicalDevices(deviceSource);
			_logicalDeviceService.UpdateInMotionLockoutLevel();
		}

		private void UpdatePrimaryLogicalDevices(IEnumerable<ILogicalDeviceSourceDirect> changedDeviceSources)
		{
			IEnumerable<ILogicalDeviceSourceDirect> changedDeviceSources2 = changedDeviceSources;
			ILogicalDeviceManager deviceManager = _logicalDeviceService.DeviceManager;
			if (deviceManager == null)
			{
				return;
			}
			foreach (ILogicalDevice item in deviceManager.FindLogicalDevices((ILogicalDevice ld) => ld.IsAssociatedWithDeviceSource(changedDeviceSources2)))
			{
				item.UpdateDeviceOnline();
				TaggedLog.Debug("LogicalDeviceSourceDirectManager", $"UpdatePrimaryLogicalDevices for ActiveConnection={item.ActiveConnection} {item}");
			}
		}

		private void UpdatePrimaryLogicalDevices(ILogicalDeviceSourceDirect changedDeviceSource)
		{
			ILogicalDeviceSourceDirect changedDeviceSource2 = changedDeviceSource;
			ILogicalDeviceManager deviceManager = _logicalDeviceService.DeviceManager;
			if (deviceManager == null)
			{
				return;
			}
			foreach (ILogicalDevice item in deviceManager.FindLogicalDevices((ILogicalDevice ld) => ld.IsAssociatedWithDeviceSource(changedDeviceSource2)))
			{
				item.UpdateDeviceOnline();
				TaggedLog.Debug("LogicalDeviceSourceDirectManager", $"UpdatePrimaryLogicalDevices for ActiveConnection={item.ActiveConnection} {item}");
			}
		}
	}
}
