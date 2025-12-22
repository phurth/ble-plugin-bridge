using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using ids.portable.ble.BleAdapter;
using ids.portable.ble.BleScanner;
using ids.portable.ble.Exceptions;
using ids.portable.ble.Platforms.Android;
using ids.portable.ble.Platforms.Shared;
using ids.portable.ble.Platforms.Shared.BleManager;
using ids.portable.ble.Platforms.Shared.ManufacturingData;
using ids.portable.ble.Platforms.Shared.ScanResults;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;
using Java.Lang.Reflect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace ids.portable.ble.BleManager
{
	internal class BleManager : BleManagerNative, IBleManager
	{
		private const string LogTag = "BleManager";

		private const int OperationTimeoutMs = 10000;

		private static readonly object _lock = new object();

		private readonly IBleAdapterService _bleAdapterService;

		private readonly IServiceProvider _serviceProvider;

		private const int ReadCharacteristicTimeoutMs = 30000;

		private const int ReadCharacteristicRetryDelayMs = 2000;

		public const int MaxWriteCharacteristicQueueSize = 20;

		private readonly TaskSerialQueue _writeWithResponseCharacteristicQueue = new TaskSerialQueue(20);

		private int _debugWriteCharacteristicWithResponseCount;

		private bool _useKeySeed = true;

		private const int ConnectTimeoutMs = 20000;

		private const int ConnectRetryDelayMS = 250;

		private static readonly TimeSpan _connectWarningTimeSpan = TimeSpan.FromMilliseconds(3000.0);

		private const int MaxNonFatalErrorAttempts = 6;

		private IBleScannerService _bleScannerService => ServiceProviderServiceExtensions.GetRequiredService<IBleScannerService>(_serviceProvider);

		public bool UseKeySeed => _useKeySeed;

		public event EventHandler<BondErrorEventArgs>? NotBonded;

		public event EventHandler<BondErrorEventArgs>? PeripheralLostBondInfo;

		public event EventHandler<DeviceEventArgs> DeviceAdvertised
		{
			add
			{
				BleAdapter.DeviceAdvertised += value;
			}
			remove
			{
				BleAdapter.DeviceAdvertised -= value;
			}
		}

		public event EventHandler<DeviceEventArgs> DeviceDiscovered
		{
			add
			{
				BleAdapter.DeviceDiscovered += value;
			}
			remove
			{
				BleAdapter.DeviceDiscovered -= value;
			}
		}

		public event EventHandler<DeviceEventArgs> DeviceConnected
		{
			add
			{
				BleAdapter.DeviceConnected += value;
			}
			remove
			{
				BleAdapter.DeviceConnected -= value;
			}
		}

		public event EventHandler<DeviceEventArgs> DeviceDisconnected
		{
			add
			{
				BleAdapter.DeviceDisconnected += value;
			}
			remove
			{
				BleAdapter.DeviceDisconnected -= value;
			}
		}

		public event EventHandler<DeviceErrorEventArgs> DeviceConnectionLost
		{
			add
			{
				BleAdapter.DeviceConnectionLost += value;
			}
			remove
			{
				BleAdapter.DeviceConnectionLost -= value;
			}
		}

		public override bool Bonded(IDevice? device)
		{
			return ((device?.NativeDevice as BluetoothDevice)?.BondState ?? Bond.None) == Bond.Bonded;
		}

		public new async Task<bool> CreateBond(IDevice? device, CancellationToken cancellationToken)
		{
			if (!(device?.NativeDevice is BluetoothDevice device2))
			{
				return false;
			}
			Activity currentActivity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
			if (currentActivity == null)
			{
				return false;
			}
			using BleBondingRequest receiver = new BleBondingRequest(device2, currentActivity, cancellationToken);
			return await receiver.Task;
		}

		public override void GoToDeviceSettings()
		{
			Intent intent = new Intent();
			intent.SetAction("android.settings.BLUETOOTH_SETTINGS");
			Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.StartActivityForResult(intent, -1);
		}

		public override void BleDeviceCacheBust(IDevice device)
		{
			try
			{
				BluetoothGatt gattFromDevice = GetGattFromDevice(device);
				if (gattFromDevice == null)
				{
					TaggedLog.Warning("BleManager", "Android's Hidden Cache-Clearing returned as gatt couldn't be found.");
					return;
				}
				Method method = gattFromDevice.Class.GetMethod("refresh");
				if (method != null)
				{
					if ((bool)method.Invoke(gattFromDevice))
					{
						TaggedLog.Debug("BleManager", "Android's Hidden Cache-Clearing method was called and succeeded");
					}
					else
					{
						TaggedLog.Warning("BleManager", "Android's Hidden Cache-Clearing method was called but failed. Device services may not have been in the cache.");
					}
				}
				else
				{
					TaggedLog.Warning("BleManager", "Android's Hidden Cache-Clearing method could not be located. Method was not called. Cache was not cleared.");
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("BleManager", "Android's Hidden Cache-Clearing threw an exception. Cache was not cleared. Message: " + ex.Message);
			}
		}

		private BluetoothGatt? GetGattFromDevice(IDevice device)
		{
			if (device == null)
			{
				throw new ArgumentNullException("device");
			}
			FieldInfo field = device.GetType().GetField("_gatt", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null)
			{
				return null;
			}
			return field.GetValue(device) as BluetoothGatt;
		}

		public BleManager(IBluetoothLE ble, IBleAdapterService bleAdapterService, IServiceProvider serviceProvider)
			: base(ble)
		{
			_bleAdapterService = bleAdapterService;
			_serviceProvider = serviceProvider;
		}

		public Task<bool> UpdateRssi(IDevice? device)
		{
			if (device == null || device!.State == DeviceState.Disconnected || BleAdapter.ConnectedDevices.Count == 0)
			{
				return Task.FromResult(false);
			}
			return device!.UpdateRssiAsync();
		}

		public Task<IService?> GetServiceAsync(IDevice device, Guid serviceGuid, CancellationToken cancellationToken)
		{
			IDevice device2 = device;
			return WrapTaskAndExecuteAsync((CancellationToken ct) => device2.GetServiceAsync(serviceGuid, ct), cancellationToken);
		}

		public async Task<ICharacteristic?> GetCharacteristicAsync(IDevice device, Guid serviceGuid, Guid characteristicGuid, CancellationToken cancellationToken)
		{
			IService service = await GetServiceAsync(device, serviceGuid, cancellationToken).TryAwaitAsync();
			if (service == null)
			{
				return null;
			}
			return await GetCharacteristicAsync(device, service, characteristicGuid, cancellationToken).TryAwaitAsync();
		}

		public Task<ICharacteristic?> GetCharacteristicAsync(IDevice device, IService service, Guid characteristicGuid, CancellationToken cancellationToken)
		{
			IService service2 = service;
			return WrapTaskAndExecuteAsync((CancellationToken ct) => service2.GetCharacteristicAsync(characteristicGuid), cancellationToken);
		}

		public async Task<byte[]?> ReadCharacteristicAsync(IDevice device, Guid serviceGuid, Guid characteristicGuid, CancellationToken cancellationToken)
		{
			IService service = await GetServiceAsync(device, serviceGuid, cancellationToken).TryAwaitAsync();
			if (service == null)
			{
				return null;
			}
			ICharacteristic characteristic = await GetCharacteristicAsync(device, service, characteristicGuid, cancellationToken).TryAwaitAsync();
			if (characteristic == null)
			{
				return null;
			}
			byte[] array;
			try
			{
				array = await ReadCharacteristicAsync(characteristic, cancellationToken).TryAwaitAsync();
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("Code=15"))
				{
					throw new NotBondedException();
				}
				throw;
			}
			if (array == null)
			{
				if (Bonded(device))
				{
					throw new PeripheralLostBondInfoException();
				}
				throw new NotBondedException();
			}
			if (array.Length == 0)
			{
				return null;
			}
			return array;
		}

		public async Task<byte[]?> ReadCharacteristicAsync(ICharacteristic? characteristic, CancellationToken cancellationToken)
		{
			ICharacteristic characteristic2 = characteristic;
			if (characteristic2 == null)
			{
				return null;
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				try
				{
					return await WrapTaskAndExecuteAsync(async delegate(CancellationToken token)
					{
						(byte[], int) tuple = await characteristic2.ReadAsync(token).ConfigureAwait(false);
						if (tuple.Item2 != 0)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 1);
							defaultInterpolatedStringHandler.AppendLiteral("with result code ");
							defaultInterpolatedStringHandler.AppendFormatted(tuple.Item2);
							defaultInterpolatedStringHandler.AppendLiteral(".");
							throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
						}
						return tuple.Item1;
					}, cancellationToken);
				}
				catch (Exception ex)
				{
					if (ex.InnerException is OperationCanceledException || ex.Message.Contains("Code=15"))
					{
						if (stopwatch.ElapsedMilliseconds >= 30000)
						{
							throw new NotBondedException();
						}
						await Task.Delay(2000, cancellationToken);
						continue;
					}
					throw;
				}
			}
		}

		public async Task<bool> WriteCharacteristicAsync(IDevice device, Guid serviceGuid, Guid characteristicGuid, byte[] data, CancellationToken cancellationToken)
		{
			IService service = await GetServiceAsync(device, serviceGuid, cancellationToken).TryAwaitAsync();
			if (service == null)
			{
				return false;
			}
			ICharacteristic characteristic = await GetCharacteristicAsync(device, service, characteristicGuid, cancellationToken).TryAwaitAsync();
			if (characteristic == null)
			{
				return false;
			}
			return await WriteCharacteristicAsync(characteristic, data, cancellationToken);
		}

		public async Task<bool> WriteCharacteristicAsync(ICharacteristic characteristic, byte[] data, CancellationToken cancellationToken)
		{
			characteristic.WriteType = CharacteristicWriteType.WithoutResponse;
			if (characteristic.WriteType != CharacteristicWriteType.WithoutResponse)
			{
				throw new ArgumentException("characteristic", "Given characteristic isn't configured for write without response.");
			}
			return await characteristic.WriteAsync(data, cancellationToken).ConfigureAwait(false) == 0;
		}

		public bool WriteCharacteristic(ICharacteristic characteristic, byte[] data)
		{
			lock (_lock)
			{
				characteristic.WriteType = CharacteristicWriteType.WithoutResponse;
				characteristic.WriteAsync(data, CancellationToken.None);
			}
			return true;
		}

		public Task<bool> WriteCharacteristicWithResponse(ICharacteristic characteristic, byte[] data)
		{
			return WriteCharacteristicWithResponseAsync(characteristic, data, CancellationToken.None);
		}

		public async Task<bool> WriteCharacteristicWithResponseAsync(IDevice device, Guid serviceGuid, Guid characteristicGuid, byte[] data, CancellationToken cancellationToken)
		{
			IService service = await GetServiceAsync(device, serviceGuid, cancellationToken).TryAwaitAsync();
			if (service == null)
			{
				return false;
			}
			ICharacteristic characteristic = await GetCharacteristicAsync(device, service, characteristicGuid, cancellationToken).TryAwaitAsync();
			if (characteristic == null)
			{
				return false;
			}
			return await WriteCharacteristicWithResponseAsync(characteristic, data, cancellationToken);
		}

		public async Task<bool> WriteCharacteristicWithResponseAsync(ICharacteristic characteristic, byte[] data, CancellationToken cancellationToken)
		{
			ICharacteristic characteristic2 = characteristic;
			byte[] data2 = data;
			int debugCount = Interlocked.Increment(ref _debugWriteCharacteristicWithResponseCount);
			try
			{
				using (await _writeWithResponseCharacteristicQueue.GetLock(cancellationToken))
				{
					return await WrapTaskAndExecuteAsync(async delegate
					{
						if (cancellationToken.IsCancellationRequested)
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(45, 2);
							defaultInterpolatedStringHandler2.AppendLiteral("Write Characteristic ");
							defaultInterpolatedStringHandler2.AppendFormatted(characteristic2.Id);
							defaultInterpolatedStringHandler2.AppendLiteral(" With Response CANCELED ");
							defaultInterpolatedStringHandler2.AppendFormatted(debugCount);
							TaggedLog.Information("BleManager", defaultInterpolatedStringHandler2.ToStringAndClear());
							return false;
						}
						characteristic2.WriteType = CharacteristicWriteType.WithResponse;
						return await characteristic2.WriteAsync(data2, CancellationToken.None) == 0;
					}, CancellationToken.None);
				}
			}
			catch (IndexOutOfRangeException)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(94, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Write Characteristic ");
				defaultInterpolatedStringHandler.AppendFormatted(characteristic2.Id);
				defaultInterpolatedStringHandler.AppendLiteral(" With Response FINISH ");
				defaultInterpolatedStringHandler.AppendFormatted(debugCount);
				defaultInterpolatedStringHandler.AppendLiteral(" Failed: because too many items in the write queue ");
				defaultInterpolatedStringHandler.AppendFormatted(20);
				Exception ex = new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
				TaggedLog.Information("BleManager", ex.Message);
				throw ex;
			}
			catch (OperationCanceledException)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Write Characteristic ");
				defaultInterpolatedStringHandler.AppendFormatted(characteristic2.Id);
				defaultInterpolatedStringHandler.AppendLiteral(" With Response CANCELED ");
				defaultInterpolatedStringHandler.AppendFormatted(debugCount);
				TaggedLog.Information("WriteCharacteristicWithResponseAsync", defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
			catch (TimeoutException)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Write Characteristic ");
				defaultInterpolatedStringHandler.AppendFormatted(characteristic2.Id);
				defaultInterpolatedStringHandler.AppendLiteral(" With Response TIMEOUT ");
				defaultInterpolatedStringHandler.AppendFormatted(debugCount);
				TaggedLog.Information("WriteCharacteristicWithResponseAsync", defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
			catch (Exception ex5)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Write Characteristic ");
				defaultInterpolatedStringHandler.AppendFormatted(characteristic2.Id);
				defaultInterpolatedStringHandler.AppendLiteral(" With Response FINISH ");
				defaultInterpolatedStringHandler.AppendFormatted(debugCount);
				defaultInterpolatedStringHandler.AppendLiteral(" Failed: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex5.Message);
				Error("WriteCharacteristicWithResponseAsync", defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
		}

		public Task<bool> StartCharacteristicUpdatesAsync(ICharacteristic characteristic)
		{
			ICharacteristic characteristic2 = characteristic;
			return WrapTaskAndExecuteAsync((CancellationToken ct) => characteristic2.StartUpdatesAsync().TryAwaitAsync(), CancellationToken.None);
		}

		public void StopCharacteristicUpdates(ICharacteristic characteristic)
		{
			characteristic.StopUpdatesAsync();
		}

		private string ResolveCancellationMessage(CancellationToken userCt, CancellationToken timeoutCt)
		{
			if (userCt.IsCancellationRequested)
			{
				return "Canceled by user";
			}
			if (!timeoutCt.IsCancellationRequested)
			{
				return "Operation canceled";
			}
			return "Operation timed out";
		}

		private static async Task<TValue> WrapTaskAndExecuteAsync<TValue>(Func<CancellationToken, Task<TValue>> taskFactory, CancellationToken cancellationToken)
		{
			TaskCompletionSource<TValue> operationCancelTcs = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
			using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			timeoutCts.CancelAfter(10000);
			CancellationToken token = timeoutCts.Token;
			Task<TValue> task;
			lock (_lock)
			{
				task = taskFactory(token);
			}
			using (token.Register(delegate
			{
				operationCancelTcs.TrySetCanceled();
			}))
			{
				return (await Task.WhenAny(operationCancelTcs.Task, task)).Result;
			}
		}

		private static void Error(string source, string message)
		{
			TaggedLog.Error("BleManager", source + ": " + message);
		}

		public void SetUseKeySeed(bool useKeySeed)
		{
			_useKeySeed = useKeySeed;
		}

		public async Task<IDevice?> TryConnectToDeviceAsync(Guid deviceId, CancellationToken cancellationToken)
		{
			try
			{
				return await InternalConnectToDeviceAsync(deviceId, cancellationToken);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 4);
				defaultInterpolatedStringHandler.AppendLiteral("ConnectToDeviceAsync for ");
				defaultInterpolatedStringHandler.AppendFormatted(deviceId);
				defaultInterpolatedStringHandler.AppendLiteral(" failed: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.GetType().Name);
				defaultInterpolatedStringHandler.AppendLiteral(" - ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				defaultInterpolatedStringHandler.AppendLiteral("\n");
				defaultInterpolatedStringHandler.AppendFormatted(ex.StackTrace);
				TaggedLog.Warning("BleManager", defaultInterpolatedStringHandler.ToStringAndClear());
				return null;
			}
		}

		public async Task<IDevice> ConnectToDeviceAsync(Guid deviceId, CancellationToken cancellationToken)
		{
			try
			{
				return (await InternalConnectToDeviceAsync(deviceId, cancellationToken).ConfigureAwait(false)) ?? throw new Exception("Unable to connect to device (connection returned null)");
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 4);
				defaultInterpolatedStringHandler.AppendLiteral("ConnectToDeviceAsync for ");
				defaultInterpolatedStringHandler.AppendFormatted(deviceId);
				defaultInterpolatedStringHandler.AppendLiteral(" failed: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.GetType().Name);
				defaultInterpolatedStringHandler.AppendLiteral(" - ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				defaultInterpolatedStringHandler.AppendLiteral("\n");
				defaultInterpolatedStringHandler.AppendFormatted(ex.StackTrace);
				TaggedLog.Warning("BleManager", defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
		}

		public async Task<IDevice> ConnectToDeviceAsync(IBleManagerConnectionParameters connectionParams, CancellationToken cancellationToken)
		{
			try
			{
				return (await InternalConnectToDeviceAsync(connectionParams, cancellationToken).ConfigureAwait(false)) ?? throw new Exception("Unable to connect to device (connection returned null)");
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 3);
				defaultInterpolatedStringHandler.AppendLiteral("ConnectToDeviceAsync for failed: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.GetType().Name);
				defaultInterpolatedStringHandler.AppendLiteral(" - ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				defaultInterpolatedStringHandler.AppendLiteral("\n");
				defaultInterpolatedStringHandler.AppendFormatted(ex.StackTrace);
				TaggedLog.Warning("BleManager", defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
		}

		public Task<IDevice?> ConnectToNonLippertDeviceAsync(Guid deviceId, CancellationToken cancellationToken)
		{
			return BleAdapter.ConnectToKnownDeviceAsync(deviceId, new ConnectParameters(autoConnect: true, forceBleTransport: true), cancellationToken);
		}

		private async Task<IDevice?> InternalConnectToDeviceAsync(Guid deviceId, CancellationToken cancellationToken)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(5, 2);
			defaultInterpolatedStringHandler.AppendFormatted("InternalConnectToDeviceAsync");
			defaultInterpolatedStringHandler.AppendLiteral(" for ");
			defaultInterpolatedStringHandler.AppendFormatted(deviceId);
			TaggedLog.Information("BleManager", defaultInterpolatedStringHandler.ToStringAndClear());
			if (!Singleton<BleManagerConnectionCache>.Instance.TryGetValue(deviceId, out var connectionParams))
			{
				IBleScanResult bleScanResult = await _bleScannerService.TryGetDeviceAsync<IBleScanResult>(deviceId, cancellationToken);
				if (bleScanResult == null)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to find device ");
					defaultInterpolatedStringHandler.AppendFormatted(connectionParams.DeviceId);
					defaultInterpolatedStringHandler.AppendLiteral(" with a scan");
					throw new BleManagerConnectionDeviceNotFoundException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				uint? keySeedCypher = null;
				if (bleScanResult is IKeySeedDeviceScanResult keySeedDeviceScanResult)
				{
					keySeedCypher = keySeedDeviceScanResult.KeySeedCypher;
				}
				connectionParams = (IBleManagerConnectionParameters)((!(bleScanResult is IPairableDeviceScanResult pairableDeviceScanResult)) ? ((object)new BleManagerConnectionParameters(deviceId, bleScanResult.DeviceName, PairingMethod.None, keySeedCypher)) : ((object)new BleManagerConnectionParameters(deviceId, bleScanResult.DeviceName, pairableDeviceScanResult.PairingMethod, keySeedCypher)));
				Singleton<BleManagerConnectionCache>.Instance.CacheConnectionParameters(connectionParams);
			}
			return await InternalConnectToDeviceAsync(connectionParams, cancellationToken);
		}

		private async Task<IDevice?> InternalConnectToDeviceAsync(IBleManagerConnectionParameters connectionParams, CancellationToken cancellationToken)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 2);
			defaultInterpolatedStringHandler.AppendFormatted("InternalConnectToDeviceAsync");
			defaultInterpolatedStringHandler.AppendLiteral(" from parameters: ");
			defaultInterpolatedStringHandler.AppendFormatted(connectionParams.DeviceId);
			TaggedLog.Information("BleManager", defaultInterpolatedStringHandler.ToStringAndClear());
			int connectionTimeout = connectionParams.ConnectionTimeoutMs.GetValueOrDefault(20000);
			int totalCurrentAttempts = 0;
			await _bleAdapterService.BleServicesEnabledCheckAsync(TimeSpan.FromMilliseconds(connectionTimeout), cancellationToken).ConfigureAwait(false);
			IDevice bleDevice = null;
			using CancellationTokenSource connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			connectCts.CancelAfter(connectionTimeout);
			do
			{
				try
				{
					ConnectParameters connectParameters = new ConnectParameters(autoConnect: false, forceBleTransport: true);
					bleDevice = await BleAdapter.ConnectToKnownDeviceAsync(connectionParams.DeviceId, connectParameters, connectCts.Token).ConfigureAwait(false);
					BleDeviceCacheBust(bleDevice);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex4)
				{
					Exception ex2 = ex4;
					int num = totalCurrentAttempts + 1;
					totalCurrentAttempts = num;
					if (ex2.Message.Contains("133"))
					{
						await TaskExtension.TryDelay(250, cancellationToken);
						continue;
					}
					if (ex2.Message.Contains("Code=14"))
					{
						throw new PeripheralLostBondInfoException();
					}
					IBleScanResult bleScanResult = await _bleScannerService.TryGetDeviceAsync<IBleScanResult>(connectionParams.DeviceId, cancellationToken);
					if (bleScanResult == null)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Unable to find device ");
						defaultInterpolatedStringHandler.AppendFormatted(connectionParams.DeviceId);
						defaultInterpolatedStringHandler.AppendLiteral(" with a scan");
						throw new BleManagerConnectionDeviceNotFoundException(defaultInterpolatedStringHandler.ToStringAndClear(), ex2);
					}
					if (bleScanResult is IConnectionCountScanResult connectionCountScanResult)
					{
						BleConnectionCount? connectionCount = connectionCountScanResult.ConnectionCount;
						if (connectionCount.HasValue && connectionCount.Value.CurrentConnections >= connectionCount.Value.MaxConnections)
						{
							throw new BleManagerConnectionDeviceTooManyConnectionsException(bleScanResult, ex2);
						}
					}
					ExceptionDispatchInfo.Capture((ex4 as Exception) ?? throw ex4).Throw();
				}
			}
			while (bleDevice == null && totalCurrentAttempts < 6);
			if (bleDevice == null)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to connect to ");
				defaultInterpolatedStringHandler.AppendFormatted(connectionParams.DeviceId);
				throw new BleManagerConnectionException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (bleDevice.State != DeviceState.Connected)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Device disconnected during the connection process ");
				defaultInterpolatedStringHandler.AppendFormatted(bleDevice.Id);
				defaultInterpolatedStringHandler.AppendLiteral("/");
				defaultInterpolatedStringHandler.AppendFormatted(bleDevice.Name);
				throw new BleManagerConnectionException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			try
			{
				if (connectionParams.PairingMethod != PairingMethod.None)
				{
					bool flag = !Bonded(bleDevice);
					if (flag)
					{
						flag = !(await CreateBond(bleDevice, CancellationToken.None));
					}
					if (flag)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(61, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Phone is not bonded to peripheral ");
						defaultInterpolatedStringHandler.AppendFormatted(connectionParams.DeviceId);
						defaultInterpolatedStringHandler.AppendLiteral("/");
						defaultInterpolatedStringHandler.AppendFormatted(connectionParams.DeviceName);
						defaultInterpolatedStringHandler.AppendLiteral(". User need to pair again.");
						throw new NotBondedException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					if (connectionParams.KeySeedCypher.HasValue)
					{
						TaggedLog.Debug("BleManager", "Key/Seed Exchange - Starting");
						switch (await this.PerformKeySeedExchange(bleDevice, connectionParams.KeySeedCypher.Value, cancellationToken))
						{
						case BleDeviceKeySeedExchangeResult.Failed:
							throw new BleManagerConnectionKeySeedException("Key/Seed Exchange - Failed");
						case BleDeviceKeySeedExchangeResult.Cancelled:
							throw new BleManagerConnectionKeySeedCancelledException("Key/Seed Exchange - Cancelled");
						default:
							TaggedLog.Debug("BleManager", "Key/Seed Exchange - Success");
							break;
						}
					}
				}
			}
			catch (NotBondedException ex5)
			{
				NotBondedException ex = ex5;
				TaggedLog.Warning("BleManager", ex.Message);
				try
				{
					this.NotBonded?.Invoke(this, new BondErrorEventArgs(connectionParams.DeviceId));
				}
				catch
				{
				}
				IBleScanResult obj2 = await _bleScannerService.TryGetDeviceAsync<IBleScanResult>(connectionParams.DeviceId, cancellationToken);
				if (obj2 == null)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to find device ");
					defaultInterpolatedStringHandler.AppendFormatted(connectionParams.DeviceId);
					defaultInterpolatedStringHandler.AppendLiteral(" with a scan");
					throw new BleManagerConnectionDeviceNotFoundException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
				}
				if (obj2 is IPairableDeviceScanResult pairableDeviceScanResult && !pairableDeviceScanResult.PairingEnabled)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Pairing not enabled for ");
					defaultInterpolatedStringHandler.AppendFormatted(connectionParams.DeviceId);
					throw new NotBondedPairingNotEnabledException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				ExceptionDispatchInfo.Capture((ex5 as Exception) ?? throw ex5).Throw();
			}
			catch (PeripheralLostBondInfoException)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(88, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Peripheral ");
				defaultInterpolatedStringHandler.AppendFormatted(connectionParams.DeviceId);
				defaultInterpolatedStringHandler.AppendLiteral("/");
				defaultInterpolatedStringHandler.AppendFormatted(connectionParams.DeviceName);
				defaultInterpolatedStringHandler.AppendLiteral(" lost the binding information. User need to bond information and pair again.");
				string message = defaultInterpolatedStringHandler.ToStringAndClear();
				TaggedLog.Warning("BleManager", message);
				try
				{
					this.PeripheralLostBondInfo?.Invoke(this, new BondErrorEventArgs(connectionParams.DeviceId));
				}
				catch
				{
				}
				throw new PeripheralLostBondInfoException(message);
			}
			catch (BleManagerConnectionKeySeedException ex7)
			{
				TaggedLog.Warning("BleManager", ex7.Message);
				throw;
			}
			catch (BleManagerConnectionKeySeedCancelledException ex8)
			{
				TaggedLog.Warning("BleManager", ex8.Message);
				throw;
			}
			catch (Exception ex9)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error connecting to ");
				defaultInterpolatedStringHandler.AppendFormatted(bleDevice.Id);
				defaultInterpolatedStringHandler.AppendLiteral("(");
				defaultInterpolatedStringHandler.AppendFormatted(bleDevice.Name);
				defaultInterpolatedStringHandler.AppendLiteral("): ");
				defaultInterpolatedStringHandler.AppendFormatted(ex9.Message);
				TaggedLog.Warning("BleManager", defaultInterpolatedStringHandler.ToStringAndClear());
				await TryDisconnectDeviceAsync(bleDevice);
				throw;
			}
			return bleDevice;
		}

		public async Task TryDisconnectDeviceAsync(IDevice device)
		{
			try
			{
				await DisconnectDeviceAsync(device);
			}
			catch (OperationCanceledException)
			{
			}
			catch (TimeoutException)
			{
			}
			catch (Exception ex3)
			{
				TaggedLog.Error("BleManager", "Unexpected error while disconnecting " + ex3.Message);
			}
		}

		public Task DisconnectDeviceAsync(IDevice device)
		{
			IDevice device2 = device;
			if (device2 == null)
			{
				TaggedLog.Warning("BleManager", "DisconnectDeviceAsync: device is null, cannot disconnect");
				return Task.FromResult(false);
			}
			try
			{
				return WrapTaskAndExecuteAsync(async delegate
				{
					await BleAdapter.DisconnectDeviceAsync(device2).TryAwaitAsync();
					return true;
				}, CancellationToken.None);
			}
			catch
			{
				return Task.FromResult(false);
			}
		}
	}
}
