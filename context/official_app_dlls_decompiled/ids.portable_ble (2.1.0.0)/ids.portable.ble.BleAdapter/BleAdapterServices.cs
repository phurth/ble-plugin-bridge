using System;
using System.Threading;
using System.Threading.Tasks;
using ids.portable.ble.Exceptions;
using IDS.Portable.Common;
using Microsoft.Extensions.Logging;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace ids.portable.ble.BleAdapter
{
	internal class BleAdapterServices : IBleAdapterService
	{
		private readonly object _lock = new object();

		private readonly IServiceProvider _serviceProvider;

		private readonly IBluetoothLE _ble;

		private readonly ILogger _logger;

		private TaskCompletionSource<BluetoothState>? _bleOnCompletionSource;

		private Func<IServiceProvider, Task<bool>> IsBluetoothPermissionGrantedAsync { get; }

		public BleAdapterServices(IServiceProvider serviceProvider, IBluetoothLE ble, ILogger<BleAdapterServices> logger, Func<IServiceProvider, Task<bool>> isBluetoothPermissionGrantedAsync)
		{
			_serviceProvider = serviceProvider;
			_ble = ble;
			_logger = logger;
			IsBluetoothPermissionGrantedAsync = isBluetoothPermissionGrantedAsync;
		}

		public async Task BleServicesEnabledCheckAsync(TimeSpan timeout, CancellationToken cancellationToken, bool validateLocationServices = true)
		{
			if (_ble.State != BluetoothState.On)
			{
				LoggerExtensions.LogInformation(_logger, "BLE Waiting For Adapter Being Turned On");
				try
				{
					await GetBluetoothOnTaskCompletionSource().TryWaitAsync(cancellationToken, (int)timeout.TotalMilliseconds, updateTcs: false, BluetoothState?.Unknown);
					if (_ble.State != BluetoothState.On)
					{
						throw new BleUnableToTurnOnException("Waited for BLE to turn on, but it's still off");
					}
				}
				catch (Exception innerException)
				{
					throw new BleUnableToTurnOnException("Unable to turn On BLE ", innerException);
				}
			}
			bool flag = validateLocationServices;
			if (flag)
			{
				flag = !(await IsBluetoothPermissionGrantedAsync(_serviceProvider));
			}
			if (flag)
			{
				throw new BlePermissionException("Bluetooth permission not granted");
			}
		}

		private void BleOnStateChanged(object? sender, BluetoothStateChangedArgs e)
		{
			BluetoothState newState = e.NewState;
			lock (_lock)
			{
				TaskCompletionSource<BluetoothState> bluetoothOnTaskCompletionSource = GetBluetoothOnTaskCompletionSource();
				if (newState == BluetoothState.On)
				{
					if (!bluetoothOnTaskCompletionSource.Task.IsCompleted)
					{
						bluetoothOnTaskCompletionSource.TrySetResult(newState);
					}
				}
				else if (bluetoothOnTaskCompletionSource.Task.IsCompleted)
				{
					_bleOnCompletionSource = null;
				}
			}
		}

		private TaskCompletionSource<BluetoothState> GetBluetoothOnTaskCompletionSource()
		{
			lock (_lock)
			{
				if (_bleOnCompletionSource == null || _bleOnCompletionSource!.Task.IsCanceled || _bleOnCompletionSource!.Task.IsFaulted)
				{
					_bleOnCompletionSource = new TaskCompletionSource<BluetoothState>(TaskCreationOptions.RunContinuationsAsynchronously);
				}
				if (_bleOnCompletionSource!.Task.IsCompleted)
				{
					if (_ble.State == BluetoothState.On && _bleOnCompletionSource!.Task.Result == BluetoothState.On)
					{
						return _bleOnCompletionSource;
					}
					_bleOnCompletionSource = new TaskCompletionSource<BluetoothState>();
				}
				if (_ble.State == BluetoothState.On)
				{
					_bleOnCompletionSource!.TrySetResult(BluetoothState.On);
				}
				return _bleOnCompletionSource;
			}
		}
	}
}
