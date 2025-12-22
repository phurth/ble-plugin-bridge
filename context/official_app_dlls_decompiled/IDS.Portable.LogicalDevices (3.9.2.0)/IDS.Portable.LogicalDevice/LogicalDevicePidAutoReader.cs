using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidAutoReader<TLogicalDevicePid, TValue> : CommonDisposableNotifyPropertyChanged, IBackgroundOperation where TLogicalDevicePid : ILogicalDevicePid<TValue>
	{
		public delegate void ValueWasSetAction(TValue value, bool didChange);

		private const string LogTag = "LogicalDevicePidAutoReader";

		public const int DefaultAutoRefreshTimeMs = 10000;

		private readonly ValueWasSetAction? _valueWasSetAction;

		private readonly BackgroundOperation<bool> _autoRefreshBackgroundOperation;

		protected static Stopwatch _stopwatch = Stopwatch.StartNew();

		private bool _hasValueBeenLoaded;

		private TimeSpan _valueLastUpdatedTimestamp;

		private TValue _value;

		private bool _pidNotSupported;

		public ILogicalDevicePid<TValue> LogicalDevicePid { get; }

		public int AutoRefreshTimeMs { get; }

		public TValue DefaultValue { get; }

		public bool HasValueBeenLoaded
		{
			get
			{
				return _hasValueBeenLoaded;
			}
			private set
			{
				SetBackingField(ref _hasValueBeenLoaded, value, "HasValueBeenLoaded");
			}
		}

		public TimeSpan MetricValueLastUpdated { get; private set; } = TimeSpan.Zero;


		public uint MetricValueUpdateCount { get; private set; }

		public TValue Value
		{
			get
			{
				return _value;
			}
			set
			{
				MetricValueUpdateCount++;
				MetricValueLastUpdated = _stopwatch.Elapsed - _valueLastUpdatedTimestamp;
				_valueLastUpdatedTimestamp = _stopwatch.Elapsed;
				bool didChange = SetBackingField(ref _value, value, "Value");
				HasValueBeenLoaded = true;
				NotifyPropertyChanged("MetricValueUpdateCount");
				NotifyPropertyChanged("MetricValueLastUpdated");
				_valueWasSetAction?.Invoke(value, didChange);
			}
		}

		public LogicalDevicePidAutoReader(TLogicalDevicePid logicalDevicePid, int autoRefreshTimeMs = 10000, TValue defaultValue = default(TValue), ValueWasSetAction? valueUpdated = null)
		{
			TLogicalDevicePid val = logicalDevicePid;
			if (val == null)
			{
				throw new ArgumentNullException("logicalDevicePid");
			}
			LogicalDevicePid = val;
			int num = logicalDevicePid.PidReadTimeoutSec * 1000;
			AutoRefreshTimeMs = Math.Max(autoRefreshTimeMs, (int)((double)num * 1.5));
			_valueWasSetAction = valueUpdated;
			MetricValueUpdateCount = 0u;
			_valueLastUpdatedTimestamp = _stopwatch.Elapsed;
			DefaultValue = defaultValue;
			_value = defaultValue;
			_autoRefreshBackgroundOperation = new BackgroundOperation<bool>((BackgroundOperation<bool>.BackgroundOperationFunc)AutoRefreshBackgroundOperation);
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_autoRefreshBackgroundOperation.Stop();
		}

		private async Task AutoRefreshBackgroundOperation(bool autoStop, CancellationToken cancellationToken)
		{
			while (!base.IsDisposed && !cancellationToken.IsCancellationRequested && !_pidNotSupported)
			{
				try
				{
					Value = await LogicalDevicePid.ReadAsync(cancellationToken);
					if (autoStop)
					{
						TaggedLog.Debug("LogicalDevicePidAutoReader", $"{LogicalDevicePid} auto stop read because value was loaded/updated {Value}");
					}
				}
				catch (TimeoutException)
				{
				}
				catch (TaskCanceledException)
				{
				}
				catch (PhysicalDeviceNotFoundException)
				{
				}
				catch (LogicalDevicePidNotSupportedException ex4)
				{
					TaggedLog.Debug("LogicalDevicePidAutoReader", $"Read Pid {LogicalDevicePid}: PID not supported: {ex4.Message}");
					_pidNotSupported = true;
				}
				catch (LogicalDevicePidInvalidValueException ex5)
				{
					TaggedLog.Debug("LogicalDevicePidAutoReader", $"Read Pid {LogicalDevicePid}: PID value was invalid and is being ignored: {ex5.Message}");
				}
				catch (Exception ex6)
				{
					TaggedLog.Error("LogicalDevicePidAutoReader", $"Load Pid {LogicalDevicePid}: {ex6.Message}");
				}
				finally
				{
					if (!base.IsDisposed)
					{
						await TaskExtension.TryDelay(AutoRefreshTimeMs, cancellationToken);
					}
				}
			}
		}

		public void Start(bool autoStop)
		{
			if (!base.IsDisposed)
			{
				_autoRefreshBackgroundOperation.Start(autoStop);
			}
		}

		public void Start()
		{
			Start(autoStop: false);
		}

		public void Stop()
		{
			if (!base.IsDisposed)
			{
				_autoRefreshBackgroundOperation.Stop();
			}
		}
	}
}
