using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using AndroidX.Work;
using ids.portable.ble.BleAdapter;
using ids.portable.ble.BleAdapter.PlatformAdapter.Android;
using ids.portable.ble.BleManager;
using ids.portable.ble.Exceptions;
using ids.portable.ble.Platforms.Shared.BleScanner;
using ids.portable.ble.Platforms.Shared.ScanResults;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;
using Java.Util.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Android;

namespace ids.portable.ble.BleScanner
{
	internal class BleScannerService : BleScannerServiceNative, IBleScannerService
	{
		private enum AndroidScanType
		{
			Stopped,
			FullScan,
			BackgroundFiltered,
			BackgroundUnfiltered
		}

		private AutoStatePreferenceListener? _prefListener;

		private bool _isListeningForAutoStateChanges;

		private bool _isAutoRunning;

		private AndroidScanType _currentScanType;

		private const short PendingIntentScanRequestCode = 422;

		private static readonly ScanSettings? _backgroundScanSettings = new ScanSettings.Builder()?.SetScanMode(Android.Bluetooth.LE.ScanMode.LowPower)?.SetCallbackType(ScanCallbackType.AllMatches)?.SetMatchMode(BluetoothScanMatchMode.Aggressive)?.SetNumOfMatches(2)?.SetReportDelay(0L)?.Build();

		private readonly IBleAdapterService _bleAdapterService;

		private readonly IAdapter _bleAdapter;

		private const string LogTag = "BleScannerService";

		private static readonly object _lock = new object();

		public const int KeepScanResultTimeMs = 60000;

		public const int GetDevicesTimeoutMs = 20000;

		public const int WaitForBleEnableTimeMs = 25000;

		public const int WaitForBleEnableFailTimeMs = 4000;

		public const int WaitToRestartScanMs = 10000;

		private readonly BackgroundOperation<Plugin.BLE.Abstractions.Contracts.ScanMode> _scannerBackgroundOperation;

		private readonly HashSet<IBleScannerCommand> _commandProcessingList = new HashSet<IBleScannerCommand>();

		private readonly BleScanResultFactoryRegistry _factoryRegistry;

		private bool _filterUsingExplicitServiceUuids;

		private readonly ConcurrentDictionary<Guid, IBleScanResult> _currentScanResultsFoundDict = new ConcurrentDictionary<Guid, IBleScanResult>();

		private readonly Watchdog _currentScanResultExpireWatchdog;

		public override bool IsExplicitServiceUuidScanningSupported { get; } = Build.VERSION.SdkInt > BuildVersionCodes.O;


		public override int ScanTimeoutMs => (int)TimeSpan.FromMinutes(4.5).TotalMilliseconds;

		public override int ScannerStopDelayMs => 0;

		public IBleScanResultFactoryRegistry FactoryRegistry => _factoryRegistry;

		public bool FilterUsingExplicitServiceUuids
		{
			get
			{
				return _filterUsingExplicitServiceUuids;
			}
			set
			{
				_filterUsingExplicitServiceUuids = value;
			}
		}

		public IEnumerable<IBleScanResult> CachedScanResults => _currentScanResultsFoundDict.Values;

		public Plugin.BLE.Abstractions.Contracts.ScanMode ScanMode
		{
			get
			{
				return _bleAdapter.ScanMode;
			}
			set
			{
				_bleAdapter.ScanMode = value;
			}
		}

		public event Action<IBleScanResult>? DidReceiveScanResult;

		public void TryStartAndroidBackgroundScanning()
		{
			try
			{
				Adapter adapter = CrossBluetoothLE.Current.Adapter as Adapter;
				Context context = Application.Context;
				Intent intent = new Intent(context, typeof(PendingIntentScanReceiver)).SetAction("ids.portable.ble.BleAdapter.PlatformAdapter.Android.PendingIntentScanReceiver.ReceiveScanResult");
				PendingIntent broadcast = PendingIntent.GetBroadcast(context, 422, intent, PendingIntentFlags.Mutable | PendingIntentFlags.UpdateCurrent);
				if (broadcast == null)
				{
					return;
				}
				if (!(adapter.GetType().GetField("_bluetoothAdapter", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(adapter) is BluetoothAdapter bluetoothAdapter))
				{
					Console.WriteLine("Failed to get _bluetoothAdapter field from Adapter.");
					return;
				}
				List<ScanFilter> list = null;
				list = new List<ScanFilter>();
				Guid[] array = Enumerable.ToArray(_factoryRegistry.RegisteredServiceUuids);
				for (int i = 0; i < array.Length; i++)
				{
					Guid guid = array[i];
					ScanFilter.Builder builder = new ScanFilter.Builder();
					builder.SetServiceUuid(ParcelUuid.FromString(guid.ToString()));
					list.Add(builder.Build());
				}
				bluetoothAdapter.BluetoothLeScanner?.StartScan(list, _backgroundScanSettings, broadcast);
			}
			catch (Exception ex)
			{
				TaggedLog.Debug("BleScannerService", "Failed to call StartBackgroundScanning via reflection. Exception: " + ex.Message);
			}
		}

		public void StartPeriodicScanRunner()
		{
			WorkManager.GetInstance(Application.Context).EnqueueUniquePeriodicWork("com.ids.ble.BLE_PERIODIC_SCAN", ExistingPeriodicWorkPolicy.Keep, PeriodicWorkRequest.Builder.From<PeriodicScanRunner>(900000L, TimeUnit.Milliseconds, 300000L, TimeUnit.Milliseconds).Build());
		}

		private void StopPeriodicScanRunner()
		{
			WorkManager.GetInstance(Application.Context).CancelUniqueWork("com.ids.ble.BLE_PERIODIC_SCAN");
		}

		private void StartAndroidAutoListener()
		{
			ISharedPreferences sharedPreferences = Application.Context.GetSharedPreferences("AutoState", FileCreationMode.Private);
			bool boolean = sharedPreferences.GetBoolean("isAutoRunning", false);
			UpdateScanBehaviorBasedOnAuto(boolean);
			_prefListener = new AutoStatePreferenceListener(UpdateScanBehaviorBasedOnAuto);
			sharedPreferences.RegisterOnSharedPreferenceChangeListener(_prefListener);
		}

		private void StopAndroidAutoListener()
		{
			if (_isListeningForAutoStateChanges && _prefListener != null)
			{
				_isListeningForAutoStateChanges = false;
				Application.Context.GetSharedPreferences("AutoState", FileCreationMode.Private).UnregisterOnSharedPreferenceChangeListener(_prefListener);
				_prefListener!.Dispose();
				_prefListener = null;
			}
		}

		public void UpdateScanBehaviorBasedOnAuto(bool isAutoRunning)
		{
			_isAutoRunning = isAutoRunning;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 1);
			defaultInterpolatedStringHandler.AppendLiteral("OnSharedPreferenceChanged: IsAutoRunning = ");
			defaultInterpolatedStringHandler.AppendFormatted(_isAutoRunning);
			TaggedLog.Debug("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
			AndroidScanType currentScanType = _currentScanType;
			bool flag = (uint)currentScanType <= 1u;
			bool flag2 = isAutoRunning && _currentScanType == AndroidScanType.BackgroundUnfiltered;
			bool flag3 = !isAutoRunning && _currentScanType == AndroidScanType.BackgroundFiltered;
			if (!(flag || flag2 || flag3))
			{
				Start(filterUsingExplicitServiceUuids: true);
			}
		}

		private void StartAndroidScanner(bool scanUsingUuids)
		{
			EnsureAutoListener();
			if (!scanUsingUuids)
			{
				RunFullScan();
			}
			else if (_isAutoRunning)
			{
				RunUnfilteredBackgroundScan();
			}
			else
			{
				RunFilteredBackgroundScan();
			}
		}

		private void EnsureAutoListener()
		{
			if (!_isListeningForAutoStateChanges)
			{
				_isListeningForAutoStateChanges = true;
				StartAndroidAutoListener();
			}
		}

		private void RunFullScan()
		{
			TaggedLog.Debug("BleScannerService", "Starting Full BLE Scanner");
			StopPeriodicScanRunner();
			_currentScanType = AndroidScanType.FullScan;
			_scannerBackgroundOperation.Start(Plugin.BLE.Abstractions.Contracts.ScanMode.LowLatency);
		}

		private void RunUnfilteredBackgroundScan()
		{
			TaggedLog.Debug("BleScannerService", "Starting Unfiltered Background BLE Scanner");
			StopPeriodicScanRunner();
			_currentScanType = AndroidScanType.BackgroundUnfiltered;
			_scannerBackgroundOperation.Start(Plugin.BLE.Abstractions.Contracts.ScanMode.LowLatency);
		}

		private void RunFilteredBackgroundScan()
		{
			TaggedLog.Debug("BleScannerService", "Starting Filtered Background BLE Scanner");
			_currentScanType = AndroidScanType.BackgroundFiltered;
			StartPeriodicScanRunner();
			TryStartAndroidBackgroundScanning();
		}

		public BleScannerService(IBluetoothLE ble, IBleAdapterService bleAdapterService, IServiceProvider serviceProvider)
		{
			_bleAdapter = ble.Adapter;
			_bleAdapterService = bleAdapterService;
			_currentScanResultExpireWatchdog = new Watchdog(30000, 60000, RemoveOldScanResults, autoStartOnFirstPet: true);
			_scannerBackgroundOperation = new BackgroundOperation<Plugin.BLE.Abstractions.Contracts.ScanMode>((BackgroundOperation<Plugin.BLE.Abstractions.Contracts.ScanMode>.BackgroundOperationFunc)ScannerBackgroundOperationAsync);
			_factoryRegistry = new BleScanResultFactoryRegistry();
			ApplyScanTimeout();
			ServiceProviderServiceExtensions.GetRequiredService<IBleManager>(serviceProvider).DeviceAdvertised += BleAdapterOnDeviceAdvertised;
		}

		public void Stop()
		{
			PlatformStop();
		}

		public void Start(bool filterUsingExplicitServiceUuids = false)
		{
			_filterUsingExplicitServiceUuids = FilterUsingExplicitServiceUuids;
			PlatformStop();
			PlatformStart(filterUsingExplicitServiceUuids);
		}

		private void PlatformStart(bool filterUsingExplicitServiceUuids)
		{
			StartAndroidScanner(filterUsingExplicitServiceUuids);
		}

		private void PlatformStop()
		{
			_scannerBackgroundOperation.Stop();
			StopPeriodicScanRunner();
		}

		public void ForceDeviceRefresh(Guid deviceId)
		{
			lock (_lock)
			{
				_currentScanResultsFoundDict.TryRemove(deviceId);
			}
		}

		public async Task<TScanResult?> GetDeviceAsync<TScanResult>(Guid deviceId, CancellationToken cancelToken, string deviceName = "") where TScanResult : class, IBleScanResult
		{
			TScanResult foundScanResult = null;
			try
			{
				await GetDevicesAsync(delegate(BleScanResultOperation operation, TScanResult scanResult)
				{
					foundScanResult = scanResult;
				}, delegate(TScanResult scanResult)
				{
					if (scanResult.DeviceId != deviceId)
					{
						return BleScannerCommandControl.Skip;
					}
					switch (scanResult.HasRequiredAdvertisements)
					{
					case BleRequiredAdvertisements.Unknown:
					case BleRequiredAdvertisements.AllExist:
						return BleScannerCommandControl.IncludeAndFinish;
					default:
						return BleScannerCommandControl.Include;
					}
				}, cancelToken);
			}
			catch (Exception ex) when (ex is TimeoutException || ex is OperationCanceledException)
			{
				if (foundScanResult != null)
				{
					return foundScanResult;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to find device ");
				defaultInterpolatedStringHandler.AppendFormatted(deviceName);
				defaultInterpolatedStringHandler.AppendLiteral("(");
				defaultInterpolatedStringHandler.AppendFormatted(deviceId);
				defaultInterpolatedStringHandler.AppendLiteral(" scanner scanning ");
				defaultInterpolatedStringHandler.AppendFormatted(_scannerBackgroundOperation.Started);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Debug("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
			catch (BleServiceException ex2)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to find device ");
				defaultInterpolatedStringHandler.AppendFormatted(deviceName);
				defaultInterpolatedStringHandler.AppendLiteral("(");
				defaultInterpolatedStringHandler.AppendFormatted(deviceId);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
				TaggedLog.Debug("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
			catch (Exception innerException)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to find device ");
				defaultInterpolatedStringHandler.AppendFormatted(deviceName);
				defaultInterpolatedStringHandler.AppendLiteral("(");
				defaultInterpolatedStringHandler.AppendFormatted(deviceId);
				defaultInterpolatedStringHandler.AppendLiteral(")");
				throw new BleScannerUnableToFindDeviceException(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
			}
			return foundScanResult;
		}

		public async Task<TScanResult?> TryGetDeviceAsync<TScanResult>(Guid deviceId, CancellationToken cancelToken, string deviceName = "") where TScanResult : class, IBleScanResult
		{
			try
			{
				return await GetDeviceAsync<TScanResult>(deviceId, cancelToken, deviceName);
			}
			catch (TimeoutException)
			{
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex3)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Failed scanning for device ");
				defaultInterpolatedStringHandler.AppendFormatted(deviceName);
				defaultInterpolatedStringHandler.AppendLiteral("(");
				defaultInterpolatedStringHandler.AppendFormatted(deviceId);
				defaultInterpolatedStringHandler.AppendLiteral("): ");
				defaultInterpolatedStringHandler.AppendFormatted(ex3.Message);
				TaggedLog.Debug("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return null;
		}

		public async Task<TScanResult?> GetDeviceAsync<TScanResult>(string deviceName, CancellationToken cancelToken) where TScanResult : class, IBleScanResult
		{
			string deviceName2 = deviceName;
			TScanResult foundDevice = null;
			try
			{
				await GetDevicesAsync(delegate(BleScanResultOperation operation, TScanResult scanResult)
				{
					foundDevice = scanResult;
				}, delegate(TScanResult scanResult)
				{
					if (scanResult.DeviceName != deviceName2)
					{
						return BleScannerCommandControl.Skip;
					}
					switch (scanResult.HasRequiredAdvertisements)
					{
					case BleRequiredAdvertisements.Unknown:
					case BleRequiredAdvertisements.AllExist:
						return BleScannerCommandControl.IncludeAndFinish;
					default:
						return BleScannerCommandControl.Include;
					}
				}, cancelToken);
			}
			catch (Exception ex) when (ex is TimeoutException || ex is OperationCanceledException)
			{
				if (foundDevice == null)
				{
					throw;
				}
			}
			catch (BleServiceException)
			{
				throw;
			}
			catch (Exception innerException)
			{
				throw new BleScannerUnableToFindDeviceException("Unable to find device " + deviceName2, innerException);
			}
			return foundDevice;
		}

		public async Task<TScanResult?> TryGetDeviceAsync<TScanResult>(string deviceName, CancellationToken cancelToken) where TScanResult : class, IBleScanResult
		{
			try
			{
				return await GetDeviceAsync<TScanResult>(deviceName, cancelToken);
			}
			catch (TimeoutException)
			{
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex3)
			{
				TaggedLog.Debug("BleScannerService", "Failed scanning for device " + deviceName + "): " + ex3.Message);
			}
			return null;
		}

		public Task GetDevicesAsync<TScanResult>(Action<BleScanResultOperation, TScanResult> deviceScanned, Func<TScanResult, BleScannerCommandControl> filter, CancellationToken cancelToken) where TScanResult : IBleScanResult
		{
			BleScannerCommand<TScanResult> bleScannerCommand = new BleScannerCommand<TScanResult>(deviceScanned, filter);
			lock (_lock)
			{
				_commandProcessingList.Add(bleScannerCommand);
				bleScannerCommand.UpdateDevices(Enumerable.Where(_currentScanResultsFoundDict.Values, (IBleScanResult sr) => _factoryRegistry.IsPrimaryServiceGuidRegistered(sr?.PrimaryServiceGuid)));
				IBleScannerCommand[] array = Enumerable.ToArray(_commandProcessingList);
				foreach (IBleScannerCommand bleScannerCommand2 in array)
				{
					if (bleScannerCommand2.IsCompleted)
					{
						_commandProcessingList.Remove(bleScannerCommand2);
						bleScannerCommand2.TryDispose();
						_commandProcessingList.TryRemove(bleScannerCommand2);
					}
				}
			}
			return bleScannerCommand.CommandCompletion.WaitAsync(cancelToken, 20000, updateTcs: true);
		}

		public async Task TryGetDevicesAsync<TScanResult>(Action<BleScanResultOperation, TScanResult> deviceScanned, Func<TScanResult, BleScannerCommandControl> filter, CancellationToken cancelToken) where TScanResult : IBleScanResult
		{
			try
			{
				await GetDevicesAsync(deviceScanned, filter, cancelToken);
			}
			catch (TimeoutException)
			{
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex3)
			{
				TaggedLog.Debug("BleScannerService", "Failed scanning for devices: " + ex3.Message);
			}
		}

		protected async Task ScannerBackgroundOperationAsync(Plugin.BLE.Abstractions.Contracts.ScanMode scanMode, CancellationToken cancellationToken)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(20, 1);
			defaultInterpolatedStringHandler.AppendLiteral("BLE Scanner Started ");
			defaultInterpolatedStringHandler.AppendFormatted(scanMode);
			TaggedLog.Information("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					await _bleAdapterService.BleServicesEnabledCheckAsync(TimeSpan.FromMilliseconds(25000.0), cancellationToken);
				}
				catch (BlePermissionException)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(70, 1);
					defaultInterpolatedStringHandler.AppendLiteral("BLE Scanner, BLE permission is not granted. Waiting ");
					defaultInterpolatedStringHandler.AppendFormatted(4000);
					defaultInterpolatedStringHandler.AppendLiteral("ms to try again...");
					TaggedLog.Warning("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
					await TaskExtension.TryDelay(4000, cancellationToken);
					continue;
				}
				catch (Exception ex2)
				{
					if (!cancellationToken.IsCancellationRequested)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(71, 3);
						defaultInterpolatedStringHandler.AppendLiteral("BLE Scanner, BLE support is not enabled (");
						defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
						defaultInterpolatedStringHandler.AppendLiteral(" ");
						defaultInterpolatedStringHandler.AppendFormatted(ex2.GetType().Name);
						defaultInterpolatedStringHandler.AppendLiteral("). Waiting ");
						defaultInterpolatedStringHandler.AppendFormatted(4000);
						defaultInterpolatedStringHandler.AppendLiteral("ms to try again...");
						TaggedLog.Warning("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
						await TaskExtension.TryDelay(4000, cancellationToken);
						continue;
					}
				}
				break;
			}
			if (cancellationToken.IsCancellationRequested)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 1);
				defaultInterpolatedStringHandler.AppendLiteral("BLE Scanner start for ");
				defaultInterpolatedStringHandler.AppendFormatted(scanMode);
				defaultInterpolatedStringHandler.AppendLiteral(" canceled");
				TaggedLog.Warning("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
				return;
			}
			await StopScanningForDevicesAsync();
			ApplyScanTimeout();
			ScanMode = scanMode;
			if (!FilterUsingExplicitServiceUuids || !IsExplicitServiceUuidScanningSupported)
			{
				await StartContiniousScan(scanMode, cancellationToken);
			}
			else
			{
				await StartBackgroundScanning(cancellationToken);
			}
		}

		private async Task StartContiniousScan(Plugin.BLE.Abstractions.Contracts.ScanMode scanMode, CancellationToken cancellationToken)
		{
			bool restartScan = true;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			while (!cancellationToken.IsCancellationRequested && restartScan)
			{
				try
				{
					TaggedLog.Information("BleScannerService", "BLE Scanner calling StartScanningForDevicesAsync.");
					restartScan = false;
					await StartScanningForDevicesAsync(null, null, allowDuplicatesKey: false, cancellationToken);
					TaggedLog.Information("BleScannerService", "BLE Scanner StartScanningForDevices finished.");
					if (!cancellationToken.IsCancellationRequested)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
						defaultInterpolatedStringHandler.AppendLiteral("BLE Scanner pausing for ");
						defaultInterpolatedStringHandler.AppendFormatted(10000);
						defaultInterpolatedStringHandler.AppendLiteral("ms.");
						TaggedLog.Information("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
						restartScan = true;
						await StopScanningForDevicesAsync();
						await TaskExtension.TryDelay(10000, cancellationToken);
						if (!cancellationToken.IsCancellationRequested)
						{
							TaggedLog.Information("BleScannerService", "BLE Scanner auto restarting.");
						}
					}
				}
				catch (Exception ex)
				{
					TaggedLog.Error("BleScannerService", "BLE Scanner, Failure: " + ex.Message);
					await StopScanningForDevicesAsync();
				}
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
			defaultInterpolatedStringHandler.AppendLiteral("BLE Scanner, Stopped ");
			defaultInterpolatedStringHandler.AppendFormatted(scanMode);
			TaggedLog.Information("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
		}

		private async Task StartBackgroundScanning(CancellationToken cancellationToken)
		{
			try
			{
				Guid[] serviceUuids = Enumerable.ToArray(_factoryRegistry.RegisteredServiceUuids);
				await StartScanningForDevicesAsync(serviceUuids, null, allowDuplicatesKey: false, cancellationToken);
				TaggedLog.Information("BleScannerService", "BLE UUID Background Scanner, Started for the following UUIDs:");
				Guid[] array = serviceUuids;
				foreach (Guid guid in array)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 1);
					defaultInterpolatedStringHandler.AppendLiteral(" - ");
					defaultInterpolatedStringHandler.AppendFormatted(guid);
					TaggedLog.Information("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Error("BleScannerService", "BLE Background Scanner, Failure: " + ex.Message);
				await StopScanningForDevicesAsync();
			}
		}

		private void BleAdapterOnDeviceAdvertised(object sender, DeviceEventArgs e)
		{
			IDevice device = e.Device;
			lock (_lock)
			{
				IBleScanResult scanResult = null;
				try
				{
					if (_currentScanResultsFoundDict.TryGetValue(device.Id, out scanResult))
					{
						scanResult.UpdateScanResult(device.Rssi, device.AdvertisementRecords);
					}
					else
					{
						if (!_factoryRegistry.TryMakeScanResult(device, out scanResult))
						{
							return;
						}
						_currentScanResultsFoundDict[device.Id] = scanResult;
					}
				}
				catch (BleScannerScanResultParseException)
				{
					return;
				}
				catch (Exception ex2)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(72, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Could not process received scan result for BLE device ");
					defaultInterpolatedStringHandler.AppendFormatted(device.Id);
					defaultInterpolatedStringHandler.AppendLiteral(" with exception: ");
					defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
					defaultInterpolatedStringHandler.AppendLiteral("\n");
					defaultInterpolatedStringHandler.AppendFormatted(ex2.StackTrace);
					TaggedLog.Warning("BleScannerService", defaultInterpolatedStringHandler.ToStringAndClear());
					return;
				}
				_currentScanResultExpireWatchdog.TryPet(autoReset: true);
				foreach (IBleScannerCommand commandProcessing in _commandProcessingList)
				{
					commandProcessing.UpdateDevice(scanResult);
				}
				try
				{
					this.DidReceiveScanResult?.Invoke(scanResult);
				}
				catch (Exception ex3)
				{
					TaggedLog.Error("BleScannerService", "Error trying to perform DidReceiveScanResult: " + ex3.Message);
				}
			}
		}

		private void RemoveOldScanResults()
		{
			DateTime now = DateTime.Now;
			foreach (KeyValuePair<Guid, IBleScanResult> item in _currentScanResultsFoundDict)
			{
				if (item.Value.ScannedTimestamp.AddMilliseconds(60000.0) <= now)
				{
					_currentScanResultsFoundDict.TryRemove(item.Key);
				}
			}
		}

		public Task StartScanningForDevicesAsync(Guid[]? serviceUuids = null, Func<IDevice, bool>? deviceFilter = null, bool allowDuplicatesKey = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (serviceUuids == null || serviceUuids!.Length == 0)
			{
				return _bleAdapter.StartScanningForDevicesAsync((ScanFilterOptions)null, (Func<IDevice, bool>)null, allowDuplicatesKey: false, cancellationToken);
			}
			return _bleAdapter.StartScanningForDevicesAsync(serviceUuids, null, allowDuplicatesKey: false, cancellationToken);
		}

		public async Task StopScanningForDevicesAsync()
		{
			await _bleAdapter.StopScanningForDevicesAsync();
			if (ScannerStopDelayMs > 0)
			{
				await Task.Delay(ScannerStopDelayMs);
			}
		}

		private void ApplyScanTimeout()
		{
			_bleAdapter.ScanTimeout = ScanTimeoutMs;
		}
	}
}
