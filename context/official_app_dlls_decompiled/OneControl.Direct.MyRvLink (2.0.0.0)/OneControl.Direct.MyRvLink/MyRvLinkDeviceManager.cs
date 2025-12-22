using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceManager
	{
		private class LogicalDeviceData
		{
			private long _refreshedOnlineTimestampMs;

			private long _refreshedLockoutTimestampMs;

			private bool _isOnline;

			private IN_MOTION_LOCKOUT_LEVEL _lockoutLevel = (byte)0;

			public ILogicalDevice LogicalDevice { get; }

			public bool ShouldRemoveDevice { get; set; }

			public long TimeSinceLastRefreshedOnlineMs => LogicalDeviceFreeRunningTimer.ElapsedMilliseconds - _refreshedOnlineTimestampMs;

			public long TimeSinceLastRefreshedLockoutMs => LogicalDeviceFreeRunningTimer.ElapsedMilliseconds - _refreshedLockoutTimestampMs;

			public bool IsOnline
			{
				get
				{
					return _isOnline;
				}
				set
				{
					_refreshedOnlineTimestampMs = LogicalDeviceFreeRunningTimer.ElapsedMilliseconds;
					if (_isOnline != value)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 3);
						defaultInterpolatedStringHandler.AppendLiteral("LogicalDeviceData update IsOnline to ");
						defaultInterpolatedStringHandler.AppendFormatted(value ? "Online" : "Offline");
						defaultInterpolatedStringHandler.AppendLiteral(" for ");
						defaultInterpolatedStringHandler.AppendFormatted(LogicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.FunctionNameCommon));
						defaultInterpolatedStringHandler.AppendLiteral(" last refreshed ");
						defaultInterpolatedStringHandler.AppendFormatted(TimeSinceLastRefreshedOnlineMs);
						defaultInterpolatedStringHandler.AppendLiteral("ms");
						TaggedLog.Debug("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
						_isOnline = value;
						LogicalDevice.UpdateDeviceOnline(value);
					}
				}
			}

			public IN_MOTION_LOCKOUT_LEVEL LockoutLevel
			{
				get
				{
					return _lockoutLevel;
				}
				set
				{
					_refreshedLockoutTimestampMs = LogicalDeviceFreeRunningTimer.ElapsedMilliseconds;
					if (_lockoutLevel != value)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(64, 3);
						defaultInterpolatedStringHandler.AppendLiteral("LogicalDeviceData update LockoutLevel to ");
						defaultInterpolatedStringHandler.AppendFormatted(value);
						defaultInterpolatedStringHandler.AppendLiteral(" for ");
						defaultInterpolatedStringHandler.AppendFormatted(LogicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.FunctionNameCommon));
						defaultInterpolatedStringHandler.AppendLiteral(" last refreshed ");
						defaultInterpolatedStringHandler.AppendFormatted(TimeSinceLastRefreshedLockoutMs);
						defaultInterpolatedStringHandler.AppendLiteral("ms");
						TaggedLog.Debug("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
						_lockoutLevel = value;
						LogicalDevice.UpdateInTransitLockout();
					}
				}
			}

			public LogicalDeviceData(ILogicalDevice logicalDevice)
			{
				LogicalDevice = logicalDevice;
				_refreshedOnlineTimestampMs = LogicalDeviceFreeRunningTimer.ElapsedMilliseconds;
				_refreshedLockoutTimestampMs = LogicalDeviceFreeRunningTimer.ElapsedMilliseconds;
			}
		}

		private const string LogTag = "MyRvLinkDeviceManager";

		private string LogPrefix;

		private readonly object _lock = new object();

		private readonly IDirectConnectionMyRvLink _directConnectionMyRvLink;

		private readonly ConcurrentDictionary<ILogicalDeviceId, LogicalDeviceData> _logicalDeviceDict = new ConcurrentDictionary<ILogicalDeviceId, LogicalDeviceData>();

		private const long AutoOfflineTimeMs = 4000L;

		private const long AutoRemoveInTransitLockoutTimeMs = 4000L;

		public ILogicalDeviceService LogicalDeviceService { get; }

		public LogicalDeviceChassisInfo? LogicalDeviceDefaultChassisInfo => Enumerable.FirstOrDefault(Enumerable.Select(_logicalDeviceDict.Values, (LogicalDeviceData logicalDeviceData) => logicalDeviceData.LogicalDevice), (ILogicalDevice logicalDevice) => (byte)logicalDevice.LogicalId.DeviceType == 39) as LogicalDeviceChassisInfo;

		public MyRvLinkDeviceManager(ILogicalDeviceService logicalDeviceService, IDirectConnectionMyRvLink directConnectionMyRvLink)
		{
			_directConnectionMyRvLink = directConnectionMyRvLink;
			LogicalDeviceService = logicalDeviceService;
			LogPrefix = directConnectionMyRvLink.LogPrefix;
		}

		public bool IsLogicalDeviceOnline(ILogicalDeviceId? logicalDeviceId)
		{
			if (logicalDeviceId == null)
			{
				return false;
			}
			return _logicalDeviceDict.TryGetValue(logicalDeviceId)?.IsOnline ?? false;
		}

		public void UpdateLogicalDeviceOnline(ILogicalDeviceId? logicalDeviceId, bool isOnline)
		{
			if (logicalDeviceId == null)
			{
				return;
			}
			if (!_logicalDeviceDict.TryGetValue(logicalDeviceId, out var logicalDeviceData))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(80, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to update online state for logical device because it doesn't exist yet: ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceId);
				TaggedLog.Debug("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				if (logicalDeviceData.IsOnline != isOnline)
				{
					LogicalDeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
				}
				logicalDeviceData.IsOnline = isOnline;
			}
		}

		public void TakeDevicesOfflineIfNeeded(bool forceOffline)
		{
			bool flag = false;
			foreach (KeyValuePair<ILogicalDeviceId, LogicalDeviceData> item in _logicalDeviceDict)
			{
				if ((forceOffline || item.Value.TimeSinceLastRefreshedOnlineMs >= 4000) && item.Value.IsOnline)
				{
					flag = true;
					item.Value.IsOnline = false;
					item.Value.LogicalDevice.UpdateInTransitLockout();
				}
			}
			if (flag)
			{
				LogicalDeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
			}
		}

		internal void RemoveOfflineDevices()
		{
			foreach (LogicalDeviceData value in _logicalDeviceDict.Values)
			{
				if (value.LogicalDevice.ActiveConnection == LogicalDeviceActiveConnection.Offline)
				{
					value.ShouldRemoveDevice = true;
				}
			}
		}

		public IN_MOTION_LOCKOUT_LEVEL GetInTransitLockoutLevel(ILogicalDeviceId? logicalDeviceId)
		{
			if (logicalDeviceId == null)
			{
				return (byte)0;
			}
			return _logicalDeviceDict.TryGetValue(logicalDeviceId)?.LockoutLevel ?? ((IN_MOTION_LOCKOUT_LEVEL)(byte)0);
		}

		public void UpdateInTransitLockoutLevel(ILogicalDeviceId? logicalDeviceId, IN_MOTION_LOCKOUT_LEVEL lockoutLevel)
		{
			if (logicalDeviceId != null)
			{
				if (!_logicalDeviceDict.TryGetValue(logicalDeviceId, out var logicalDeviceData))
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(81, 2);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" Unable to update Lockout Level for logical device because it doesn't exist yet: ");
					defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceId);
					TaggedLog.Debug("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				else
				{
					logicalDeviceData.LockoutLevel = lockoutLevel;
				}
			}
		}

		public void RemoveInTransitLockoutLevel(bool forceRemoveLockout)
		{
			foreach (KeyValuePair<ILogicalDeviceId, LogicalDeviceData> item in _logicalDeviceDict)
			{
				if (forceRemoveLockout || item.Value.TimeSinceLastRefreshedLockoutMs >= 4000)
				{
					item.Value.LockoutLevel = (byte)0;
				}
			}
		}

		public ILogicalDevice? GetLogicalDevice(IMyRvLinkDeviceForLogicalDevice device)
		{
			ILogicalDeviceId logicalDeviceId = device.LogicalDeviceId;
			if (logicalDeviceId == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(83, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to get logical device for ");
				defaultInterpolatedStringHandler.AppendFormatted(device);
				defaultInterpolatedStringHandler.AppendLiteral(" because it doesn't have a Logical Device Id yet.");
				TaggedLog.Debug("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
				return null;
			}
			ILogicalDeviceManager deviceManager = LogicalDeviceService.DeviceManager;
			if (deviceManager == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to get logical device for ");
				defaultInterpolatedStringHandler.AppendFormatted(device);
				defaultInterpolatedStringHandler.AppendLiteral(" because ");
				defaultInterpolatedStringHandler.AppendFormatted("DeviceManager");
				defaultInterpolatedStringHandler.AppendLiteral(" is null");
				TaggedLog.Warning("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
				return null;
			}
			lock (_lock)
			{
				if (_logicalDeviceDict.TryGetValue(logicalDeviceId, out var logicalDeviceData))
				{
					if (logicalDeviceData.ShouldRemoveDevice)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(58, 3);
						defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
						defaultInterpolatedStringHandler.AppendLiteral(" ");
						defaultInterpolatedStringHandler.AppendFormatted("GetLogicalDevice");
						defaultInterpolatedStringHandler.AppendLiteral(" Found device but it is in the process of being removed: ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceId);
						TaggedLog.Warning("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
						return null;
					}
					if (!logicalDeviceData.LogicalDevice.IsDisposed)
					{
						return logicalDeviceData.LogicalDevice;
					}
					_logicalDeviceDict.TryRemove(logicalDeviceId);
				}
				ILogicalDevice logicalDevice = deviceManager.FindLogicalDevice(logicalDeviceId) ?? AddLogicalDevice(device);
				if (logicalDevice == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 3);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted("GetLogicalDevice");
					defaultInterpolatedStringHandler.AppendLiteral(" unable to find or create LogicalDevice ");
					defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceId);
					TaggedLog.Warning("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
					_logicalDeviceDict.TryRemove(logicalDeviceId);
					return null;
				}
				_logicalDeviceDict[logicalDeviceId] = new LogicalDeviceData(logicalDevice);
				return logicalDevice;
			}
		}

		internal ILogicalDevice? AddLogicalDevice(IMyRvLinkDeviceForLogicalDevice device)
		{
			ILogicalDeviceId logicalDeviceId = device.LogicalDeviceId;
			if (logicalDeviceId == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(83, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to add logical device for ");
				defaultInterpolatedStringHandler.AppendFormatted(device);
				defaultInterpolatedStringHandler.AppendLiteral(" because it doesn't have a Logical Device Id yet.");
				TaggedLog.Debug("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
				return null;
			}
			lock (_lock)
			{
				ILogicalDevice logicalDevice = null;
				try
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 2);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" AddLogicalDevice find LogicalDevice for ");
					defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceId);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					TaggedLog.Debug("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
					logicalDevice = LogicalDeviceService?.DeviceManager?.AddLogicalDevice(logicalDeviceId, device.RawDefaultCapability, _directConnectionMyRvLink, (ILogicalDevice attemptAutoRenameForLogicalDevice) => true);
					if (logicalDevice == null)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 1);
						defaultInterpolatedStringHandler.AppendLiteral("logical device was null for ");
						defaultInterpolatedStringHandler.AppendFormatted(device);
						throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 2);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" AddLogicalDevice found/created LogicalDevice ");
					defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
					TaggedLog.Debug("MyRvLinkDeviceManager", defaultInterpolatedStringHandler.ToStringAndClear());
					logicalDevice.UpdateDeviceCapability(device.RawDefaultCapability);
				}
				catch (Exception ex)
				{
					TaggedLog.Error("MyRvLinkDeviceManager", LogPrefix + " AddLogicalDevice failed " + ex.Message);
				}
				return logicalDevice;
			}
		}
	}
}
