using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceCircuitId : CommonDisposable, ILogicalDeviceCircuitId, INotifyPropertyChanged, ICommonDisposable, IDisposable
	{
		private const string LogTag = "LogicalDeviceCircuitId";

		private readonly ILogicalDevice _logicalDevice;

		private readonly object _locker = new object();

		private readonly CancellationTokenSource _cts = new CancellationTokenSource();

		private readonly CancellationToken _ct;

		private LogicalDeviceCircuitIdWriteTracker? _queuedWrite;

		private bool _hasValueBeenLoaded;

		private bool _isWriting;

		private CIRCUIT_ID _value;

		public bool HasValueBeenLoaded
		{
			get
			{
				return _hasValueBeenLoaded;
			}
			private set
			{
				if (_hasValueBeenLoaded != value)
				{
					_hasValueBeenLoaded = value;
					OnPropertyChanged("HasValueBeenLoaded");
				}
			}
		}

		public bool IsWriting
		{
			get
			{
				return _isWriting;
			}
			private set
			{
				if (_isWriting != value)
				{
					_isWriting = value;
					OnPropertyChanged("IsWriting");
				}
			}
		}

		public CIRCUIT_ID LastValue { get; private set; } = 0u;


		public CIRCUIT_ID Value
		{
			get
			{
				return _value;
			}
			private set
			{
				HasValueBeenLoaded = true;
				if ((uint)_value != (uint)value)
				{
					LastValue = _value;
					_value = value;
					OnPropertyChanged("Value");
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		public LogicalDeviceCircuitId(ILogicalDevice logicalDevice)
		{
			_logicalDevice = logicalDevice;
			_ct = _cts.Token;
		}

		public void UpdateValue(CIRCUIT_ID circuitId)
		{
			bool flag = false;
			lock (_locker)
			{
				if (!IsWriting)
				{
					flag = true;
				}
			}
			if (flag)
			{
				Value = circuitId;
			}
		}

		public Task<LogicalDeviceCircuitIdWriteResult> WriteValueAsync(CIRCUIT_ID circuitId, CancellationToken cancellationToken)
		{
			if (base.IsDisposed || _ct.IsCancellationRequested)
			{
				return Task.FromResult(LogicalDeviceCircuitIdWriteResult.CancelledViaDispose);
			}
			using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_ct, cancellationToken);
			CancellationToken token = cancellationTokenSource.Token;
			TaskCompletionSource<LogicalDeviceCircuitIdWriteResult> taskCompletionSource = new TaskCompletionSource<LogicalDeviceCircuitIdWriteResult>();
			lock (_locker)
			{
				if (base.IsDisposed)
				{
					TaggedLog.Debug("LogicalDeviceCircuitId", "Attempt to write to a disposed LogicalDeviceCircuitId.");
					return Task.FromResult(LogicalDeviceCircuitIdWriteResult.Cancelled);
				}
				_queuedWrite?.Result.SetResult(((uint)_queuedWrite!.CircuitId != (uint)circuitId) ? LogicalDeviceCircuitIdWriteResult.Preempted : LogicalDeviceCircuitIdWriteResult.PreemptedWithSameValue);
				_queuedWrite = new LogicalDeviceCircuitIdWriteTracker(circuitId, token, taskCompletionSource);
				BeginWriteIfNotWriting();
			}
			Value = circuitId;
			return taskCompletionSource.Task;
		}

		private void BeginWriteIfNotWriting()
		{
			lock (_locker)
			{
				if (!base.IsDisposed && !IsWriting)
				{
					IsWriting = true;
					LogicalDeviceCircuitIdWriteTracker tracker = new LogicalDeviceCircuitIdWriteTracker(_queuedWrite!.CircuitId, _queuedWrite!.CancellationToken, _queuedWrite!.Result);
					_queuedWrite = null;
					WriteTaskAsync(tracker);
				}
			}
		}

		private async Task WriteTaskAsync(LogicalDeviceCircuitIdWriteTracker tracker)
		{
			while (IsWriting)
			{
				try
				{
					await new LogicalDevicePid(_logicalDevice, PID.IDS_CAN_CIRCUIT_ID, LogicalDeviceSessionType.Diagnostic).WriteValueAsync((uint)tracker.CircuitId, tracker.CancellationToken).ConfigureAwait(false);
					tracker.Result.SetResult(LogicalDeviceCircuitIdWriteResult.Completed);
				}
				catch (OperationCanceledException)
				{
					tracker.Result.SetResult(_cts.IsCancellationRequested ? LogicalDeviceCircuitIdWriteResult.CancelledViaDispose : LogicalDeviceCircuitIdWriteResult.Cancelled);
				}
				catch (Exception ex2)
				{
					TaggedLog.Debug("LogicalDeviceCircuitId", "LogicalDeviceCircuitId - Error writing Circuit ID PID " + ex2.Message);
					tracker.Result.SetResult(LogicalDeviceCircuitIdWriteResult.Failed);
				}
				lock (_locker)
				{
					if (_queuedWrite != null)
					{
						tracker = new LogicalDeviceCircuitIdWriteTracker(_queuedWrite!.CircuitId, _queuedWrite!.CancellationToken, _queuedWrite!.Result);
						_queuedWrite = null;
					}
					else
					{
						IsWriting = false;
					}
				}
			}
		}

		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public override void Dispose(bool disposing)
		{
			_cts?.TryCancelAndDispose();
			this.PropertyChanged = null;
		}
	}
}
