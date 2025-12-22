using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using OneControl.Devices;
using OneControl.Devices.TankSensor.Mopeka;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.Mopeka
{
	public class MopekaSensor : CommonDisposable
	{
		private const string LogTag = "MopekaSensor";

		private readonly TimeSpan OnlineTimeout = TimeSpan.FromSeconds(60.0);

		private readonly ILogicalDeviceService _deviceService;

		private readonly ILogicalDeviceManager _deviceManager;

		private readonly ILogicalDeviceSourceDirect _deviceSource;

		private readonly IBleService _bleService;

		private readonly ILPSettingsRepository _lpSettingsRepository;

		private readonly LogicalDeviceTankSensor? _logicalDevice;

		private readonly Watchdog? _takeOfflineWatchdog;

		private float _maximumTankLevelInMm;

		private DateTime _lastSeenScanResult = DateTime.MinValue;

		public static readonly TimeSpan StatusUpdateThrottleTime = TimeSpan.FromSeconds(1.0);

		private int _lowBatteryAlertCount;

		private int _lowTankLevelAlertCount;

		public static readonly int BadQualityUpperLimit = 66;

		public static readonly TimeSpan StabilizationTimeout = TimeSpan.FromSeconds(60.0);

		public static readonly int SamplesToAverage = 3;

		public static readonly byte LowBatteryNotificationThreshold = 25;

		private DateTime? _lastBatteryFaultNotificationSentDateTime;

		private DateTime? _lastLowTankFaultNotificationSentDateTime;

		private CancellationTokenSource? _cts;

		private Task? _stabilizationTask;

		private bool _isStabilizing;

		private int _lastTankLevelThreshold;

		private readonly FixedSizedConcurrentQueue<int> _tankLevelSamples = new FixedSizedConcurrentQueue<int>(SamplesToAverage);

		private const int BatteryFaultNotificationLimitInMinutes = 30;

		private const int LowTankFaultNotificationLimitInMinutes = 30;

		private const int MaximumNumberOfBatteryFaultAlertsPerPeriod = 3;

		private const int MaximumNumberOfLowTankFaultAlertsPerPeriod = 3;

		private static readonly TimeSpan MaximumNumberOfBatteryFaultAlertsPeriod = TimeSpan.FromDays(1.0);

		private static readonly TimeSpan MaximumNumberOfLowTankAlertsPeriod = TimeSpan.FromDays(1.0);

		private List<DateTime> _lowBatteryFaultTimes;

		private List<DateTime> _lowTankFaultTimes;

		public SensorConnectionMopeka SensorConnection { get; }

		public MAC ShortMAC { get; }

		public bool IsOnline => DateTime.Now - _lastSeenScanResult < OnlineTimeout;

		public FUNCTION_NAME FUNCTION_NAME => _logicalDevice!.LogicalId.FunctionName;

		public byte FunctionInstance => (byte)_logicalDevice!.LogicalId.FunctionInstance;

		private MopekaSensor(ILogicalDeviceService deviceService, ILogicalDeviceSourceDirect deviceSource, IBleService bleService, ILPSettingsRepository lpSettingsRepository, SensorConnectionMopeka sensorConnection)
		{
			SensorConnection = sensorConnection;
			MAC macAddress = sensorConnection.MacAddress;
			FunctionName defaultFunctionName = sensorConnection.DefaultFunctionName;
			byte defaultFunctionInstance = sensorConnection.DefaultFunctionInstance;
			_deviceService = deviceService;
			_deviceManager = deviceService.DeviceManager ?? throw new ArgumentException("DeviceManager is null, please check your dependencies");
			_deviceSource = deviceSource;
			_bleService = bleService;
			_lpSettingsRepository = lpSettingsRepository;
			_lowBatteryFaultTimes = new List<DateTime>();
			_lowTankFaultTimes = new List<DateTime>();
			ShortMAC = macAddress;
			LogicalDeviceId logicalDeviceId = new LogicalDeviceId((byte)10, 0, defaultFunctionName.ToFunctionName(), defaultFunctionInstance, PRODUCT_ID.BOTTLECHECK_WIRELESS_LP_TANK_SENSOR, ShortMAC);
			ILogicalDevice logicalDevice = _deviceManager.FindLogicalDeviceMatchingPhysicalHardware(logicalDeviceId.MakeDeviceId((byte?)(byte)0, (byte)0), ShortMAC);
			if (logicalDevice != null)
			{
				logicalDevice.AddDeviceSource(_deviceSource);
			}
			else
			{
				logicalDevice = _deviceManager.AddLogicalDevice(logicalDeviceId, 0, _deviceSource, (ILogicalDevice ld) => true);
			}
			_logicalDevice = logicalDevice as LogicalDeviceTankSensor;
			_takeOfflineWatchdog = new Watchdog(OnlineTimeout, TakeOffline, autoStartOnFirstPet: true);
			_bleService.Scanner.DidReceiveScanResult += OnDidReceiveScanResult;
		}

		public static async Task<MopekaSensor> Create(ILogicalDeviceService deviceService, ILogicalDeviceSourceDirect deviceSource, IBleService bleService, ILPSettingsRepository lpSettingsRepository, SensorConnectionMopeka sensorConnection)
		{
			_ = sensorConnection.MacAddress;
			FunctionName defaultFunctionName = sensorConnection.DefaultFunctionName;
			byte defaultFunctionInstance = sensorConnection.DefaultFunctionInstance;
			int defaultTankSizeId = sensorConnection.DefaultTankSizeId;
			float defaultTankHeightInMm = sensorConnection.DefaultTankHeightInMm;
			bool defaultIsNotificationEnabled = sensorConnection.DefaultIsNotificationEnabled;
			int defaultNotificationThreshold = sensorConnection.DefaultNotificationThreshold;
			float defaultAccelXOffset = sensorConnection.DefaultAccelXOffset;
			float defaultAccelYOffset = sensorConnection.DefaultAccelYOffset;
			TankHeightUnits defaultPreferredUnits = sensorConnection.DefaultPreferredUnits;
			MopekaSensor sensor = new MopekaSensor(deviceService, deviceSource, bleService, lpSettingsRepository, sensorConnection);
			if (await lpSettingsRepository.HasLPSettings(sensor._logicalDevice))
			{
				MopekaSensor mopekaSensor = sensor;
				mopekaSensor._maximumTankLevelInMm = (await lpSettingsRepository.GetTankSize(sensor._logicalDevice)).TankHeightInMm;
			}
			else
			{
				ILPTankSize size = ((defaultTankSizeId != LPTankSizes.ArbitraryTankSizeId) ? ((ILPTankSize)LPTankSizes.GetById(defaultTankSizeId)) : ((ILPTankSize)new ArbitraryTankSize(defaultTankHeightInMm)));
				await lpSettingsRepository.CreateSettings(sensor._logicalDevice, LPTankName.GetByFunctionNameAndInstance(defaultFunctionName, defaultFunctionInstance), size, defaultIsNotificationEnabled, defaultNotificationThreshold, defaultAccelXOffset, defaultAccelYOffset, defaultPreferredUnits);
				sensor._maximumTankLevelInMm = size.TankHeightInMm;
				sensor._logicalDevice?.Rename(defaultFunctionName.ToFunctionName(), defaultFunctionInstance);
			}
			return sensor;
		}

		private void OnDidReceiveScanResult(IBleScanResult scanResult)
		{
			if (scanResult is MopekaScanResult mopekaScanResult && mopekaScanResult.ShortMAC.Equals(ShortMAC))
			{
				UpdateLogicalDevice(mopekaScanResult);
				UpdateAlertData(mopekaScanResult);
			}
		}

		private void UpdateAlertData(MopekaScanResult scanResult)
		{
			if (_logicalDevice == null)
			{
				TaggedLog.Error("MopekaSensor", "Cannot update Mopeka alert data as logical device is null");
				return;
			}
			if (ShouldGenerateLowBatteryAlert(scanResult))
			{
				_lowBatteryAlertCount++;
				_lastBatteryFaultNotificationSentDateTime = DateTime.UtcNow;
				_lowBatteryFaultTimes.Add(DateTime.UtcNow);
				_logicalDevice!.UpdateAlert("LowBatteryAlert", isActive: true, _lowBatteryAlertCount);
			}
			else
			{
				_lowBatteryAlertCount = 0;
				_logicalDevice!.UpdateAlert("LowBatteryAlert", isActive: false, _lowBatteryAlertCount);
			}
			CheckTankLevelsAndAlert(scanResult);
		}

		private async void CreateTankLevelAlertUpdate()
		{
			if (_lpSettingsRepository == null || base.IsDisposed)
			{
				return;
			}
			await _lpSettingsRepository.SetFaulted(LPFaultType.Tank, _logicalDevice, isFaulted: false);
			if (!_lastLowTankFaultNotificationSentDateTime.HasValue || (DateTime.UtcNow - _lastLowTankFaultNotificationSentDateTime.Value).TotalMinutes > 30.0)
			{
				int count = _lowTankFaultTimes.Count;
				if (count < 3 || _lowTankFaultTimes[count - 3] < DateTime.UtcNow - MaximumNumberOfLowTankAlertsPeriod)
				{
					_lowTankLevelAlertCount++;
					_lastLowTankFaultNotificationSentDateTime = DateTime.UtcNow;
					_lowTankFaultTimes.Add(DateTime.UtcNow);
					_logicalDevice!.UpdateAlert("TankLevelAlert", isActive: true, _lowTankLevelAlertCount);
				}
			}
		}

		private void ResetTankLevelAlert()
		{
			_lowTankLevelAlertCount = 0;
			_logicalDevice!.UpdateAlert("TankLevelAlert", isActive: false, _lowTankLevelAlertCount);
		}

		private bool ShouldGenerateLowBatteryAlert(MopekaScanResult scanResult)
		{
			try
			{
				if (!(_logicalDevice?.BatteryLevel).HasValue || _logicalDevice?.BatteryLevel.Value > LowBatteryNotificationThreshold)
				{
					return false;
				}
				if (!_lastBatteryFaultNotificationSentDateTime.HasValue || (DateTime.UtcNow - _lastBatteryFaultNotificationSentDateTime.Value).TotalMinutes > 30.0)
				{
					int count = _lowBatteryFaultTimes.Count;
					if (count < 3 || _lowBatteryFaultTimes[count - 3] < DateTime.UtcNow - MaximumNumberOfBatteryFaultAlertsPeriod)
					{
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Error("MopekaSensor", "An error occurred when trying to determine whether we should generate a low battery notification, " + ex.Message);
			}
			return false;
		}

		private async void CheckTankLevelsAndAlert(MopekaScanResult scanResult)
		{
			if (_lpSettingsRepository != null && _logicalDevice != null && await _lpSettingsRepository.IsThresholdNotificationEnabled(_logicalDevice) && _logicalDevice!.MeasurementQuality > BadQualityUpperLimit)
			{
				_tankLevelSamples.Enqueue(_logicalDevice!.Level);
				double averageTankLevel = Enumerable.Average(_tankLevelSamples);
				int notificationThreshold = await _lpSettingsRepository.GetNotificationThreshold(_logicalDevice);
				if (notificationThreshold != _lastTankLevelThreshold)
				{
					_lastTankLevelThreshold = notificationThreshold;
					await _lpSettingsRepository.SetFaulted(LPFaultType.Tank, _logicalDevice, isFaulted: false);
				}
				if (averageTankLevel > (double)notificationThreshold)
				{
					ResetTankLevelAlert();
					CancelStabilizationTimer();
				}
				else if (!(await _lpSettingsRepository.IsFaulted(LPFaultType.Tank, _logicalDevice)))
				{
					StartStabilizationTimer(CreateTankLevelAlertUpdate);
				}
			}
		}

		private void CancelStabilizationTimer()
		{
			if (_isStabilizing)
			{
				_cts?.TryCancelAndDispose();
				_stabilizationTask?.Dispose();
				_isStabilizing = false;
			}
		}

		private void StartStabilizationTimer(Action callback)
		{
			Action callback2 = callback;
			if (!_isStabilizing)
			{
				_isStabilizing = true;
				_cts = new CancellationTokenSource();
				_stabilizationTask = Task.Delay(StabilizationTimeout, _cts!.Token).ContinueWith(delegate
				{
					_cts.TryCancelAndDispose();
					_isStabilizing = false;
					callback2();
				}, _cts!.Token);
			}
		}

		private void UpdateLogicalDevice(MopekaScanResult scanResult)
		{
			TimeSpan timeSpan = DateTime.Now - _lastSeenScanResult;
			if (timeSpan < StatusUpdateThrottleTime)
			{
				return;
			}
			_lastSeenScanResult = DateTime.Now;
			_takeOfflineWatchdog?.TryPet(autoReset: true);
			if (_deviceService.GetPrimaryDeviceSourceDirect(_logicalDevice) == _deviceSource)
			{
				if (timeSpan >= OnlineTimeout)
				{
					_logicalDevice!.UpdateDeviceOnline();
					_deviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
				}
				double num = MopekaScanResult.ConvertRawTankLevelToMillimetersForLPG(scanResult.RawTankLevel, scanResult.TemperatureInCelsius);
				LogicalDeviceTankSensorStatus logicalDeviceTankSensorStatus = new LogicalDeviceTankSensorStatus(5);
				logicalDeviceTankSensorStatus.SetLevel((byte)MathCommon.Clamp(Math.Round(num / (double)_maximumTankLevelInMm * 100.0, MidpointRounding.AwayFromZero), 0.0, 100.0));
				logicalDeviceTankSensorStatus.SetBatteryLevel((byte)scanResult.BatteryPercentage);
				logicalDeviceTankSensorStatus.SetMeasurementQuality((byte)Math.Ceiling(MathCommon.InverseLinearlyInterpolate(scanResult.MeasurementQuality, 0.0, 3.0) * 100.0));
				logicalDeviceTankSensorStatus.SetXAcceleration((byte)(-MathCommon.Clamp((sbyte)scanResult.RawXAcceleration, -127, 127)));
				logicalDeviceTankSensorStatus.SetYAcceleration((byte)MathCommon.Clamp((sbyte)scanResult.RawYAcceleration, -127, 127));
				_logicalDevice!.UpdateDeviceStatus(logicalDeviceTankSensorStatus);
			}
		}

		private void TakeOffline()
		{
			if (!_logicalDevice!.IsDisposed)
			{
				_logicalDevice!.UpdateDeviceOnline();
				_deviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
			}
		}

		public IObservable<MopekaScanResult> GetSensorData()
		{
			return Observable.Where(Observable.Select(Observable.Where(Observable.FromEvent(delegate(Action<IBleScanResult> handler)
			{
				_bleService.Scanner.DidReceiveScanResult += handler;
			}, delegate(Action<IBleScanResult> handler)
			{
				_bleService.Scanner.DidReceiveScanResult -= handler;
			}), (IBleScanResult result) => result is MopekaScanResult), (IBleScanResult result) => (MopekaScanResult)result), (MopekaScanResult result) => result.ShortMAC.Equals(ShortMAC));
		}

		public override void Dispose(bool disposing)
		{
			CancelStabilizationTimer();
			_bleService.Scanner.DidReceiveScanResult -= OnDidReceiveScanResult;
			_takeOfflineWatchdog?.TryDispose();
		}

		public void DetachFromSource()
		{
			_logicalDevice?.RemoveDeviceSource(_deviceSource);
		}
	}
}
