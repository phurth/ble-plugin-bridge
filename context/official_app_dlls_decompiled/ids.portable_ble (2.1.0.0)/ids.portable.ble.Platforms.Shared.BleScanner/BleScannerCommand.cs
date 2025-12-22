using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;

namespace ids.portable.ble.Platforms.Shared.BleScanner
{
	internal class BleScannerCommand<TScanResult> : CommonDisposable, IBleScannerCommand, ICommonDisposable, IDisposable where TScanResult : IBleScanResult
	{
		private readonly object _lockObject = new object();

		private const string LogTag = "BleScannerCommand";

		private readonly Action<BleScanResultOperation, TScanResult>? _deviceScanned;

		private readonly Func<TScanResult, BleScannerCommandControl>? _deviceScanFilter;

		private CancellationTokenSource? _autoCompleteCancellationTokenSource;

		private CancellationTokenRegistration? _autoCompleteCancellationTokenRegistration;

		public Dictionary<Guid, TScanResult> FoundDevices { get; } = new Dictionary<Guid, TScanResult>();


		public TaskCompletionSource<bool> CommandCompletion { get; }

		public bool IsCompleted => CommandCompletion.Task.IsCompleted;

		public BleScannerCommand(Action<BleScanResultOperation, TScanResult>? deviceScanned, Func<TScanResult, BleScannerCommandControl> deviceScanFilter)
		{
			_deviceScanned = deviceScanned;
			_deviceScanFilter = deviceScanFilter;
			CommandCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			_autoCompleteCancellationTokenSource = new CancellationTokenSource(20000);
			_autoCompleteCancellationTokenRegistration = _autoCompleteCancellationTokenSource!.Token.Register(ScanCompleted);
			if (deviceScanned == null)
			{
				ScanCompleted();
			}
		}

		public void UpdateDevice(IBleScanResult scanResult)
		{
			if (IsCompleted || !(scanResult is TScanResult))
			{
				return;
			}
			TScanResult val = (TScanResult)scanResult;
			bool flag = false;
			switch (_deviceScanFilter?.Invoke(val) ?? BleScannerCommandControl.Include)
			{
			default:
				return;
			case BleScannerCommandControl.IncludeAndFinish:
				flag = true;
				break;
			case BleScannerCommandControl.Skip:
				return;
			case BleScannerCommandControl.Include:
				break;
			}
			lock (_lockObject)
			{
				BleScanResultOperation arg = (FoundDevices.ContainsKey(val.DeviceId) ? BleScanResultOperation.DeviceUpdate : BleScanResultOperation.DeviceDiscovered);
				_deviceScanned?.Invoke(arg, val);
				FoundDevices[val.DeviceId] = val;
				if (flag)
				{
					ScanCompleted();
				}
			}
		}

		public void UpdateDevices(IEnumerable<IBleScanResult?> devices)
		{
			lock (_lockObject)
			{
				foreach (IBleScanResult device in devices)
				{
					if (IsCompleted)
					{
						break;
					}
					if (device != null)
					{
						UpdateDevice(device);
					}
				}
			}
		}

		public void ScanCompleted()
		{
			CancelAutoComplete();
			CommandCompletion.TrySetResult(true);
		}

		public void ScanFailed(Exception ex)
		{
			CancelAutoComplete();
			CommandCompletion.TrySetException(ex);
		}

		public void ScanCanceled()
		{
			CancelAutoComplete();
			CommandCompletion.TrySetCanceled();
		}

		public override void Dispose(bool disposing)
		{
			CancelAutoComplete();
			if (!IsCompleted)
			{
				ScanCanceled();
			}
		}

		private void CancelAutoComplete()
		{
			ref CancellationTokenRegistration? autoCompleteCancellationTokenRegistration = ref _autoCompleteCancellationTokenRegistration;
			if (autoCompleteCancellationTokenRegistration.HasValue)
			{
				autoCompleteCancellationTokenRegistration.GetValueOrDefault().TryDispose();
			}
			_autoCompleteCancellationTokenRegistration = null;
			_autoCompleteCancellationTokenSource?.TryCancelAndDispose();
			_autoCompleteCancellationTokenSource = null;
		}
	}
}
