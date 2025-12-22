using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using ids.portable.ble.Platforms.Shared.Reachability;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceDriver
{
	public abstract class BleDeviceDriverLoCap<TDeviceSource, TSensorConnection, TLogicalDevice> : IdsCanAccessoryBleScanResultDeviceIdFactory, IAccessoryBleDeviceDriverLocap<TSensorConnection, TLogicalDevice>, IAccessoryBleDeviceDriverLocap, IAccessoryBleDeviceDriver, ICommonDisposable, IDisposable where TDeviceSource : IAccessoryBleDeviceSourceLocap<TSensorConnection> where TSensorConnection : ISensorConnectionBleLocap where TLogicalDevice : class, ILogicalDeviceAccessory
	{
		private readonly IBleService _bleService;

		protected readonly TDeviceSource _sourceDirect;

		private const int UuidStatusMessageDelayMs = 35000;

		private AccessoryConnectionManager<TLogicalDevice>? _accessoryConnectionManager;

		private readonly Stopwatch _logTimeSinceLastLog = new Stopwatch();

		private Watchdog? _logReceivedUpdateWatchdog;

		private int _logAccessoryCombinedMessages;

		private IdsCanAccessoryScanResult? _logAccessoryScanResult;

		private BleDeviceReachabilityManager? _deviceReachabilityManager;

		protected abstract string LogTag { get; }

		public abstract DEVICE_TYPE BleDeviceType { get; }

		protected abstract int BleConnectionAutoCloseTimeoutMs { get; }

		protected abstract int BleConnectionRetryDelayMs { get; }

		protected abstract int BleConnectAttemptMs { get; }

		protected abstract int BleConnectTimeoutMaxMs { get; }

		public TSensorConnection SensorConnection { get; }

		public Guid BleDeviceId => SensorConnection.ConnectionGuid;

		public MAC AccessoryMacAddress => SensorConnection.AccessoryMac;

		public string SoftwarePartNumber => SensorConnection.SoftwarePartNumber;

		public string BleDeviceName => SensorConnection.ConnectionNameFriendly;

		public TLogicalDevice? LogicalDevice { get; private set; }

		public AccessoryConnectionManager<TLogicalDevice>? AccessoryConnectionManager
		{
			get
			{
				if (LogicalDevice == null || base.IsDisposed)
				{
					return null;
				}
				if (LogicalDevice!.IsDisposed)
				{
					_accessoryConnectionManager?.TryDispose();
					_accessoryConnectionManager = null;
				}
				if (_accessoryConnectionManager != null)
				{
					return _accessoryConnectionManager;
				}
				_accessoryConnectionManager = new AccessoryConnectionManager<TLogicalDevice>(_bleService, LogTag, BleConnectionAutoCloseTimeoutMs, BleConnectTimeoutMaxMs, BleConnectAttemptMs, BleConnectionRetryDelayMs, LogicalDevice, BleDeviceId, BleDeviceName);
				ConfigureAccessoryConnectionManager(_accessoryConnectionManager);
				return _accessoryConnectionManager;
			}
		}

		protected virtual int LogDelayTimeMs => 1500;

		protected virtual int LogWarningTimeMs => 60000;

		private Watchdog LogReceivedUpdateWatchdog => _logReceivedUpdateWatchdog ?? (_logReceivedUpdateWatchdog = new Watchdog(LogDelayTimeMs, LogReceivedUpdate, autoStartOnFirstPet: true));

		protected virtual TimeSpan MinimumConnectionWindowTimeSpan => TimeSpan.FromMinutes(1.0);

		protected virtual TimeSpan MaximumConnectionWindowTimeSpan => TimeSpan.FromSeconds(65535.0);

		private BleDeviceReachabilityManager DeviceReachabilityManager => _deviceReachabilityManager ?? (_deviceReachabilityManager = CreateDeviceReachabilityManager());

		private string DeviceReachabilityName
		{
			get
			{
				string connectionNameFriendly = SensorConnection.ConnectionNameFriendly;
				object obj = LogicalDevice?.DeviceName;
				if (obj == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
					defaultInterpolatedStringHandler.AppendFormatted(SensorConnection.AccessoryMac);
					obj = defaultInterpolatedStringHandler.ToStringAndClear();
				}
				return connectionNameFriendly + "(" + (string)obj + ")";
			}
		}

		public bool IsOnline => DeviceReachabilityManager.Reachability == BleDeviceReachability.Reachable;

		public event ReachabilityChangedHandler? ReachabilityChanged;

		public BleDeviceDriverLoCap(IBleService bleService, TDeviceSource sourceDirect, TSensorConnection sensorConnection)
		{
			TSensorConnection sensorConnection2 = sensorConnection;
			base._002Ector(bleService, sensorConnection2.ConnectionGuid);
			_bleService = bleService;
			_sourceDirect = sourceDirect;
			SensorConnection = sensorConnection2;
			LogicalDevice = null;
			ILogicalDeviceManager? deviceManager = _sourceDirect.DeviceService.DeviceManager;
			LogicalDevice = ((deviceManager != null) ? Enumerable.FirstOrDefault(deviceManager!.FindLogicalDevices((TLogicalDevice ld) => ld.LogicalId.ProductMacAddress == sensorConnection2.AccessoryMac)) : null);
			_bleService.Scanner.FactoryRegistry.Register(this);
		}

		protected virtual void ConfigureAccessoryConnectionManager(AccessoryConnectionManager<TLogicalDevice> accessoryConnectionManager)
		{
		}

		public virtual void Update(IdsCanAccessoryScanResult accessoryScanResult)
		{
			IdsCanAccessoryStatus? accessoryStatus = accessoryScanResult.GetAccessoryStatus(AccessoryMacAddress);
			if (!accessoryStatus.HasValue)
			{
				return;
			}
			_accessoryConnectionManager?.Update(accessoryStatus.Value);
			if (accessoryStatus.Value.DeviceType != BleDeviceType)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Ignored update for ");
				defaultInterpolatedStringHandler.AppendFormatted(accessoryStatus.Value.DeviceType);
				defaultInterpolatedStringHandler.AppendLiteral(" because expecting ");
				defaultInterpolatedStringHandler.AppendFormatted(BleDeviceType);
				TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return;
			}
			if (accessoryScanResult.HasAbridgedStatus && accessoryScanResult.AccessoryAbridgedStatusDataLastUpdated > accessoryScanResult.AccessoryStatusDataLastUpdated && LogicalDevice is ILogicalDeviceWithStatusAlertsLocap logicalDeviceWithStatusAlertsLocap)
			{
				byte[] accessoryAbridgedStatus = accessoryScanResult.GetAccessoryAbridgedStatus();
				if (accessoryAbridgedStatus != null)
				{
					try
					{
						logicalDeviceWithStatusAlertsLocap.UpdateDeviceStatusAlerts(accessoryAbridgedStatus);
					}
					catch (Exception ex)
					{
						TaggedLog.Warning(LogTag, "Issue processing Status Alerts: " + ex.Message);
					}
				}
			}
			if (accessoryScanResult.AccessoryMacAddress == null)
			{
				accessoryScanResult.SetMac(AccessoryMacAddress);
			}
			TDeviceSource sourceDirect;
			if (LogicalDevice == null || !accessoryStatus.Value.IsMatchForLogicalDevice(LogicalDevice))
			{
				string logTag2 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Creating Logical Device for Direct ");
				defaultInterpolatedStringHandler.AppendFormatted(BleDeviceType);
				defaultInterpolatedStringHandler.AppendLiteral(" BLE: ");
				defaultInterpolatedStringHandler.AppendFormatted(accessoryStatus.Value.FunctionName);
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(accessoryStatus.Value.FunctionInstance);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted((ulong)AccessoryMacAddress, "X6");
				TaggedLog.Information(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
				LogicalDeviceId logicalDeviceId = new LogicalDeviceId(accessoryStatus.Value.DeviceType, 0, accessoryStatus.Value.FunctionName, accessoryStatus.Value.FunctionInstance, accessoryStatus.Value.ProductId, AccessoryMacAddress);
				sourceDirect = _sourceDirect;
				ILogicalDevice logicalDevice = sourceDirect.DeviceService.DeviceManager?.AddLogicalDevice(logicalDeviceId, accessoryStatus.Value.RawCapability, _sourceDirect, (ILogicalDevice ld) => true);
				if (!(logicalDevice is TLogicalDevice logicalDevice2) || logicalDevice.IsDisposed)
				{
					string logTag3 = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(61, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to create logical device for ");
					defaultInterpolatedStringHandler.AppendFormatted(BleDeviceType);
					defaultInterpolatedStringHandler.AppendLiteral(" with BLE Scan Result of ");
					defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult);
					TaggedLog.Warning(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
					return;
				}
				LogicalDevice = logicalDevice2;
			}
			_logAccessoryCombinedMessages++;
			_logAccessoryScanResult = accessoryScanResult;
			LogReceivedUpdateWatchdog.TryPet(autoReset: true);
			foreach (AccessoryPidStatus item in accessoryScanResult.GetAccessoryPidStatus(AccessoryMacAddress))
			{
				LogicalDevice!.SetCachedPidRawValue(item.Id, item.Value);
			}
			sourceDirect = _sourceDirect;
			if (sourceDirect.DeviceService.GetPrimaryDeviceSourceDirect(LogicalDevice) == (object)_sourceDirect)
			{
				LogicalDevice!.UpdateDeviceCapability(accessoryStatus.Value.RawCapability);
				LogicalDevice!.UpdateCircuitId(accessoryStatus.Value.CircuitId);
				if (LogicalDevice is ILogicalDeviceWithStatus logicalDeviceWithStatus)
				{
					logicalDeviceWithStatus.UpdateDeviceStatus(accessoryStatus.Value.DeviceStatus, (uint)accessoryStatus.Value.DeviceStatus.Count);
				}
				if (LogicalDevice is ILogicalDeviceWithStatusExtended logicalDeviceWithStatusExtended)
				{
					byte[] accessoryIdsCanExtendedStatus = accessoryScanResult.GetAccessoryIdsCanExtendedStatus(AccessoryMacAddress);
					if (accessoryIdsCanExtendedStatus != null)
					{
						logicalDeviceWithStatusExtended.UpdateDeviceStatusExtended(accessoryIdsCanExtendedStatus, (uint)accessoryIdsCanExtendedStatus.Length, 0);
					}
				}
			}
			DeviceReachabilityManager.DeviceReachableUntil(TimeSpan.FromSeconds(accessoryStatus.Value.ConnectionWindowSeconds));
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			if (LogicalDevice != null)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 3);
				defaultInterpolatedStringHandler.AppendFormatted(BleDeviceId);
				defaultInterpolatedStringHandler.AppendLiteral("/");
				defaultInterpolatedStringHandler.AppendFormatted(BleDeviceName);
				defaultInterpolatedStringHandler.AppendLiteral("/");
				defaultInterpolatedStringHandler.AppendFormatted(LogicalDevice!.LogicalId.ToString(LogicalDeviceIdFormat.FunctionNameCommon));
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 2);
			defaultInterpolatedStringHandler.AppendFormatted(BleDeviceId);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(BleDeviceName);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public override void Dispose(bool disposing)
		{
			this.ReachabilityChanged = null;
			DeviceReachabilityManager.TryDispose();
			_accessoryConnectionManager?.TryDispose();
			_bleService.Scanner.FactoryRegistry.UnRegister(this);
		}

		private void LogReceivedUpdate()
		{
			if (_logTimeSinceLastLog.ElapsedMilliseconds >= LogWarningTimeMs)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Received ");
				defaultInterpolatedStringHandler.AppendFormatted(BleDeviceType);
				defaultInterpolatedStringHandler.AppendLiteral(" BLE data (last seen ");
				defaultInterpolatedStringHandler.AppendFormatted(_logTimeSinceLastLog.Elapsed.TotalSeconds);
				defaultInterpolatedStringHandler.AppendLiteral("s CombinedMessages=");
				defaultInterpolatedStringHandler.AppendFormatted(_logAccessoryCombinedMessages);
				defaultInterpolatedStringHandler.AppendLiteral(")\n");
				defaultInterpolatedStringHandler.AppendFormatted(_logAccessoryScanResult);
				TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				string logTag2 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Received ");
				defaultInterpolatedStringHandler.AppendFormatted(BleDeviceType);
				defaultInterpolatedStringHandler.AppendLiteral(" BLE data (last seen ");
				defaultInterpolatedStringHandler.AppendFormatted(_logTimeSinceLastLog.Elapsed.TotalSeconds);
				defaultInterpolatedStringHandler.AppendLiteral("s CombinedMessages=");
				defaultInterpolatedStringHandler.AppendFormatted(_logAccessoryCombinedMessages);
				defaultInterpolatedStringHandler.AppendLiteral("))\n");
				defaultInterpolatedStringHandler.AppendFormatted(_logAccessoryScanResult);
				TaggedLog.Information(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_logAccessoryCombinedMessages = 0;
			_logTimeSinceLastLog.Restart();
		}

		private BleDeviceReachabilityManager CreateDeviceReachabilityManager()
		{
			return new BleDeviceReachabilityManager(MinimumConnectionWindowTimeSpan, MaximumConnectionWindowTimeSpan, DeviceReachabilityManagerOnReachabilityChanged, () => DeviceReachabilityName);
		}

		protected virtual void DeviceReachabilityManagerOnReachabilityChanged(BleDeviceReachability oldReachability, BleDeviceReachability newReachability)
		{
			switch (newReachability)
			{
			case BleDeviceReachability.Unreachable:
				LogicalDevice?.UpdateDeviceOnline(online: false);
				break;
			case BleDeviceReachability.Reachable:
				LogicalDevice?.UpdateDeviceOnline(online: true);
				break;
			default:
				LogicalDevice?.UpdateDeviceOnline();
				break;
			}
			TDeviceSource sourceDirect = _sourceDirect;
			sourceDirect.DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
			this.ReachabilityChanged?.Invoke(oldReachability, newReachability);
		}
	}
}
