using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using OneControl.Devices;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle.GenericSensor
{
	public class GenericSensorBle : IdsCanAccessoryBleScanResultDeviceIdFactory
	{
		private const string LogTag = "GenericSensorBle";

		private readonly IBleService _bleService;

		private readonly DirectGenericSensorBle _sourceDirect;

		private DateTime? _lastSeenScanResult;

		private Watchdog? _takeOfflineWatchdog;

		private static readonly TimeSpan MinimumConnectionWindowTimeSpan = TimeSpan.FromMinutes(2.0);

		private static readonly TimeSpan MaximumConnectionWindowTimeSpan = TimeSpan.FromSeconds(65535.0);

		private TimeSpan _connectionWindowTimeSpan = MinimumConnectionWindowTimeSpan;

		private const int LogDelayTimeMs = 1500;

		private const int LogWarningTimeMs = 120000;

		private readonly Stopwatch _logGenericSensorTimeSinceLastLog = new Stopwatch();

		private readonly Watchdog _logGenericSensorReceivedUpdateWatchdog;

		private int _logAccessoryCombinedMessages;

		private byte[]? _previousAlertData;

		private const int MaxNumberOfAlertBytes = 4;

		private const string GenericSensorFactoryString = "LogicalDeviceGenericSensorFactory";

		private IdsCanAccessoryScanResult? _logAccessoryScanResult;

		private const int UuidStatusMessageDelayMs = 45000;

		private AccessoryConnectionManager<ILogicalDevice>? _accessoryConnectionManager;

		public Guid BleDeviceId { get; }

		public MAC AccessoryMacAddress { get; }

		public string SoftwarePartNumber { get; private set; }

		public string BleDeviceName { get; }

		public ILogicalDevice? LogicalDevice { get; private set; }

		public LogicalDeviceGenericSensor? LogicalDeviceGenericSensor { get; private set; }

		public AccessoryConnectionManager<ILogicalDevice>? AccessoryConnectionManager
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
				return _accessoryConnectionManager ?? (_accessoryConnectionManager = new AccessoryConnectionManager<ILogicalDevice>(_bleService, "GenericSensorBle", 30000, 160000, 80000, 200, LogicalDevice, BleDeviceId, BleDeviceName));
			}
		}

		public bool IsOnline
		{
			get
			{
				if (_lastSeenScanResult.HasValue)
				{
					DateTime now = DateTime.Now;
					DateTime? lastSeenScanResult = _lastSeenScanResult;
					return now - lastSeenScanResult < _connectionWindowTimeSpan;
				}
				return false;
			}
		}

		public GenericSensorBle(IBleService bleService, DirectGenericSensorBle sourceDirect, Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			MAC accessoryMacAddress2 = accessoryMacAddress;
			base._002Ector(bleService, bleDeviceId);
			_bleService = bleService;
			_sourceDirect = sourceDirect;
			BleDeviceId = bleDeviceId;
			AccessoryMacAddress = accessoryMacAddress2;
			SoftwarePartNumber = softwarePartNumber;
			BleDeviceName = bleDeviceName;
			LogicalDevice = null;
			_lastSeenScanResult = null;
			ILogicalDeviceManager? deviceManager = _sourceDirect.DeviceService.DeviceManager;
			LogicalDevice = ((deviceManager != null) ? Enumerable.FirstOrDefault(deviceManager!.FindLogicalDevices((ILogicalDevice ld) => ld.LogicalId.ProductMacAddress == accessoryMacAddress2)) : null);
			_logGenericSensorReceivedUpdateWatchdog = new Watchdog(1500, LogGenericSensorReceivedUpdate, autoStartOnFirstPet: true);
			bleService.Scanner.FactoryRegistry.Register(this);
		}

		internal void UpdateSoftwarePartNumber(string softwarePartNumber)
		{
			SoftwarePartNumber = softwarePartNumber ?? string.Empty;
		}

		public void Update(IdsCanAccessoryScanResult accessoryScanResult)
		{
			IdsCanAccessoryStatus? accessoryStatus = accessoryScanResult.GetAccessoryStatus(AccessoryMacAddress);
			if (!accessoryStatus.HasValue)
			{
				return;
			}
			_accessoryConnectionManager?.Update(accessoryStatus.Value);
			if (accessoryScanResult.AccessoryMacAddress == null)
			{
				accessoryScanResult.SetMac(AccessoryMacAddress);
			}
			if (LogicalDevice == null || !accessoryStatus.Value.IsMatchForLogicalDevice(LogicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(57, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Creating Logical Device for Direct Generic Sensor BLE: ");
				defaultInterpolatedStringHandler.AppendFormatted(accessoryStatus.Value.FunctionName);
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(accessoryStatus.Value.FunctionInstance);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted((ulong)AccessoryMacAddress, "X6");
				TaggedLog.Information("GenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
				LogicalDeviceId logicalDeviceId = new LogicalDeviceId(accessoryStatus.Value.DeviceType, 0, accessoryStatus.Value.FunctionName, accessoryStatus.Value.FunctionInstance, accessoryStatus.Value.ProductId, AccessoryMacAddress);
				ILogicalDevice logicalDevice = _sourceDirect.DeviceService.DeviceManager?.AddLogicalDevice(logicalDeviceId, accessoryStatus.Value.RawCapability, _sourceDirect, (ILogicalDevice ld) => true);
				if (!(logicalDevice is LogicalDeviceGenericSensor logicalDeviceGenericSensor) || logicalDevice.IsDisposed)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to create LogicalDeviceGenericSensor for BLE Scan Result of ");
					defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult);
					TaggedLog.Warning("GenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
					return;
				}
				LogicalDevice = logicalDevice;
				LogicalDeviceGenericSensor = logicalDeviceGenericSensor;
				_sourceDirect.DeviceService.DeviceManager?.TagManager.AddTag(LogicalDeviceTagFavorite.DefaultFavoriteTag, logicalDevice);
			}
			if (accessoryScanResult.HasAbridgedStatus && LogicalDevice != null)
			{
				UpdateAlertInformation(accessoryScanResult.GetAccessoryAbridgedStatus());
			}
			TimeSpan timeSpan;
			if (_lastSeenScanResult.HasValue)
			{
				DateTime now = DateTime.Now;
				DateTime? lastSeenScanResult = _lastSeenScanResult;
				timeSpan = (now - lastSeenScanResult).Value;
			}
			else
			{
				timeSpan = TimeSpan.MaxValue;
			}
			TimeSpan timeSpan2 = timeSpan;
			_lastSeenScanResult = DateTime.Now;
			_logAccessoryCombinedMessages++;
			_takeOfflineWatchdog?.TryPet(autoReset: true);
			_logAccessoryScanResult = accessoryScanResult;
			_logGenericSensorReceivedUpdateWatchdog.TryPet(autoReset: true);
			foreach (AccessoryPidStatus item in accessoryScanResult.GetAccessoryPidStatus(AccessoryMacAddress))
			{
				LogicalDevice!.SetCachedPidRawValue(item.Id, item.Value);
			}
			if (_sourceDirect.DeviceService.GetPrimaryDeviceSourceDirect(LogicalDevice) == _sourceDirect)
			{
				LogicalDevice!.UpdateDeviceCapability(accessoryStatus.Value.RawCapability);
				LogicalDevice!.UpdateCircuitId(accessoryStatus.Value.CircuitId);
				TimeSpan timeSpan3 = TimeSpan.FromSeconds(MathCommon.Clamp(accessoryStatus.Value.ConnectionWindowSeconds, MinimumConnectionWindowTimeSpan.TotalSeconds, MaximumConnectionWindowTimeSpan.TotalSeconds));
				if (_takeOfflineWatchdog == null)
				{
					_connectionWindowTimeSpan = timeSpan3;
					_takeOfflineWatchdog = new Watchdog(_connectionWindowTimeSpan, TakeOffline, autoStartOnFirstPet: true);
				}
				else if (_connectionWindowTimeSpan != timeSpan3)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Ignoring Attempt to change Connection Window Timespan for ");
					defaultInterpolatedStringHandler.AppendFormatted(LogicalDevice);
					defaultInterpolatedStringHandler.AppendLiteral(" from ");
					defaultInterpolatedStringHandler.AppendFormatted(_connectionWindowTimeSpan);
					defaultInterpolatedStringHandler.AppendLiteral(" to ");
					defaultInterpolatedStringHandler.AppendFormatted(timeSpan3);
					TaggedLog.Error("GenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				if (timeSpan2 >= _connectionWindowTimeSpan)
				{
					LogicalDevice!.UpdateDeviceOnline();
					_sourceDirect.DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
				}
			}
		}

		private void LogGenericSensorReceivedUpdate()
		{
			if (_logGenericSensorTimeSinceLastLog.ElapsedMilliseconds >= 120000)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Received Generic Sensor BLE data (last seen ");
				defaultInterpolatedStringHandler.AppendFormatted(_logGenericSensorTimeSinceLastLog.Elapsed.TotalSeconds);
				defaultInterpolatedStringHandler.AppendLiteral("s CombinedMessages=");
				defaultInterpolatedStringHandler.AppendFormatted(_logAccessoryCombinedMessages);
				defaultInterpolatedStringHandler.AppendLiteral(")\n");
				defaultInterpolatedStringHandler.AppendFormatted(_logAccessoryScanResult);
				TaggedLog.Warning("GenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(66, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Received Generic Sensor BLE data (last seen ");
				defaultInterpolatedStringHandler.AppendFormatted(_logGenericSensorTimeSinceLastLog.Elapsed.TotalSeconds);
				defaultInterpolatedStringHandler.AppendLiteral("s CombinedMessages=");
				defaultInterpolatedStringHandler.AppendFormatted(_logAccessoryCombinedMessages);
				defaultInterpolatedStringHandler.AppendLiteral("))\n");
				defaultInterpolatedStringHandler.AppendFormatted(_logAccessoryScanResult);
				TaggedLog.Information("GenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (LogicalDevice != null)
			{
				TaggedLog.Information("GenericSensorBle", "  All Cached PIDs:");
				foreach (var cachedPid in LogicalDevice!.GetCachedPids())
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(9, 2);
					defaultInterpolatedStringHandler.AppendLiteral("    ");
					defaultInterpolatedStringHandler.AppendFormatted(cachedPid.Pid);
					defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
					defaultInterpolatedStringHandler.AppendFormatted(cachedPid.Value, "X");
					TaggedLog.Information("GenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			_logAccessoryCombinedMessages = 0;
			_logGenericSensorTimeSinceLastLog.Restart();
		}

		private void UpdateAlertInformation(byte[]? alertData)
		{
			if (alertData != null && alertData!.Length != 0 && LogicalDeviceGenericSensor != null && alertData!.Length <= 4 && (_previousAlertData == null || !Enumerable.SequenceEqual(_previousAlertData, alertData)))
			{
				_previousAlertData = alertData;
				LogicalDeviceGenericSensor?.UpdateAlert(alertData);
			}
		}

		private void TakeOffline()
		{
			if (LogicalDevice != null && !LogicalDevice!.IsDisposed)
			{
				LogicalDevice?.UpdateDeviceOnline();
				_sourceDirect.DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
			}
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
			_bleService.Scanner.FactoryRegistry.UnRegister(this);
			_takeOfflineWatchdog?.TryDispose();
			_accessoryConnectionManager?.TryDispose();
		}
	}
}
