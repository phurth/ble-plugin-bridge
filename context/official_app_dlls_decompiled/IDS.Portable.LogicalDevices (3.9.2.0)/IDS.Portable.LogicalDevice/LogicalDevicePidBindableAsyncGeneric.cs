using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public abstract class LogicalDevicePidBindableAsyncGeneric<TLogicalDevicePid, TValue> : BindableAsyncValue<TValue>, ILogicalDevicePidBindableAsync<TLogicalDevicePid, TValue>, IBindableAsyncValue<TValue>, INotifyPropertyChanged, ICommonDisposable, IDisposable where TLogicalDevicePid : ILogicalDevicePid where TValue : IEquatable<TValue>
	{
		private const string LogTag = "LogicalDevicePidBindableAsyncGeneric";

		public readonly bool AutoLoadPid;

		public readonly bool AutoSavePid;

		public readonly bool AutoRefreshPid;

		private CallbackTimer? _autoRefreshTimer;

		private CancellationTokenSource? _initialPidRefreshCancelTokenSource;

		private CancellationTokenSource? _autoSaveCancelTokenSource;

		private uint _readingValueCount;

		private uint _writingValueCount;

		private PropertyChangedEventHandler? _autoPropertyChanged;

		public uint ReadingValueCount
		{
			get
			{
				return _readingValueCount;
			}
			private set
			{
				if (_readingValueCount != value)
				{
					_readingValueCount = 0u;
					base.IsReading = _readingValueCount != 0;
					OnPropertyChanged("ReadingValueCount");
				}
			}
		}

		public uint WritingValueCount
		{
			get
			{
				return _writingValueCount;
			}
			private set
			{
				if (_writingValueCount != value)
				{
					_writingValueCount = 0u;
					base.IsWriting = _writingValueCount != 0;
					OnPropertyChanged("WritingValueCount");
				}
			}
		}

		public TValue ValueToUseWhenInvalidData { get; set; }

		public override TValue Value
		{
			get
			{
				return base.Value;
			}
			set
			{
				if (base.Value == null && value == null)
				{
					return;
				}
				TValue value2 = base.Value;
				if (value2 != null && value2.Equals(value))
				{
					return;
				}
				base.Value = value;
				if (!AutoSavePid || !base.HasValueBeenLoaded)
				{
					return;
				}
				_autoSaveCancelTokenSource?.TryCancelAndDispose();
				_autoSaveCancelTokenSource = new CancellationTokenSource();
				Task.Run(async delegate
				{
					try
					{
						await SaveAsync(_autoSaveCancelTokenSource!.Token);
					}
					catch (Exception ex)
					{
						TaggedLog.Debug("LogicalDevicePidBindableAsyncGeneric", "SaveAsync PID Exception " + ex.Message);
					}
				});
			}
		}

		public TLogicalDevicePid PidComponent { get; }

		protected abstract Task<TValue> ReadValueAsync(TLogicalDevicePid logicalDevicePid, CancellationToken cancellationToken);

		protected abstract Task WriteValueAsync(TLogicalDevicePid logicalDevicePid, TValue value, CancellationToken cancellationToken);

		public LogicalDevicePidBindableAsyncGeneric(TLogicalDevicePid logicalDevicePid, bool autoLoadPid, bool autoSavePid = false, bool autoRefreshPid = false, PropertyChangedEventHandler? propertyChanged = null)
		{
			AutoLoadPid = autoLoadPid;
			AutoSavePid = autoSavePid;
			AutoRefreshPid = autoRefreshPid;
			TLogicalDevicePid val = logicalDevicePid;
			if (val == null)
			{
				throw new ArgumentNullException("logicalDevicePid");
			}
			PidComponent = val;
			_autoPropertyChanged = propertyChanged;
			if (_autoPropertyChanged != null)
			{
				base.PropertyChanged += _autoPropertyChanged;
			}
			if (autoRefreshPid)
			{
				int num = logicalDevicePid.PidReadTimeoutSec * 1000;
				int msDueTime = Math.Max(10000, (int)((double)num * 1.5));
				_autoRefreshTimer = new CallbackTimer(PidBackgroundRefresh, msDueTime, repeat: true);
			}
			if (AutoLoadPid)
			{
				PidBackgroundRefresh();
			}
		}

		protected void PidBackgroundRefresh()
		{
			if (base.IsDisposed)
			{
				return;
			}
			_initialPidRefreshCancelTokenSource?.TryCancelAndDispose();
			_initialPidRefreshCancelTokenSource = null;
			if (PidComponent.PropertyId == PID.UNKNOWN)
			{
				return;
			}
			_initialPidRefreshCancelTokenSource = new CancellationTokenSource();
			CancellationToken cancelToken = _initialPidRefreshCancelTokenSource!.Token;
			TValue value;
			Task.Run(async delegate
			{
				try
				{
					value = await ReadValueAsync(PidComponent, cancelToken);
					if (!base.IsDisposed && !cancelToken.IsCancellationRequested)
					{
						MainThread.RequestMainThreadAction(delegate
						{
							if (!base.HasValueBeenLoaded || AutoRefreshPid)
							{
								if (base.IsDisposed)
								{
									TaggedLog.Debug("LogicalDevicePidBindableAsyncGeneric", $"PID {PidComponent.PropertyId} UpdateValue aborted");
								}
								else
								{
									UpdateValue(value, valueLoaded: true, valueValid: true);
								}
							}
						});
					}
				}
				catch (TimeoutException)
				{
					TaggedLog.Debug("LogicalDevicePidBindableAsyncGeneric", "PidBackgroundRefresh " + PidComponent.PropertyId.Name + ": Timeout");
				}
				catch (TaskCanceledException)
				{
					TaggedLog.Debug("LogicalDevicePidBindableAsyncGeneric", "PidBackgroundRefresh " + PidComponent.PropertyId.Name + ": Operation Canceled");
				}
				catch (LogicalDevicePidNotSupportedException ex3)
				{
					TaggedLog.Debug("LogicalDevicePidBindableAsyncGeneric", "PidBackgroundRefresh " + PidComponent.PropertyId.Name + ": PID not supported: " + ex3.Message);
					_autoRefreshTimer?.TryCancelAndDispose();
					_autoRefreshTimer = null;
				}
				catch (PhysicalDeviceNotFoundException ex4)
				{
					TaggedLog.Debug("LogicalDevicePidBindableAsyncGeneric", "PidBackgroundRefresh " + PidComponent.PropertyId.Name + ": Physical device not found: " + ex4.Message);
				}
				catch (LogicalDevicePidInvalidValueException)
				{
					MainThread.RequestMainThreadAction(delegate
					{
						if (!base.HasValueBeenLoaded)
						{
							UpdateValue(ValueToUseWhenInvalidData, valueLoaded: true, valueValid: false);
						}
					});
				}
				catch (Exception ex6)
				{
					TaggedLog.Error("LogicalDevicePidBindableAsyncGeneric", "PidBackgroundRefresh " + PidComponent.PropertyId.Name + ": " + ex6.Message);
				}
				finally
				{
					_initialPidRefreshCancelTokenSource?.TryCancelAndDispose();
					_initialPidRefreshCancelTokenSource = null;
				}
			}, cancelToken);
		}

		public override async Task LoadAsync(CancellationToken cancellationToken)
		{
			if (base.IsDisposed)
			{
				return;
			}
			try
			{
				ReadingValueCount++;
				UpdateValue(await ReadValueAsync(PidComponent, cancellationToken), valueLoaded: true, valueValid: true);
			}
			catch (LogicalDevicePidInvalidValueException)
			{
				UpdateValue(ValueToUseWhenInvalidData, valueLoaded: true, valueValid: false);
				throw;
			}
			finally
			{
				if (ReadingValueCount != 0)
				{
					ReadingValueCount--;
				}
			}
		}

		public override async Task SaveAsync(CancellationToken cancellationToken)
		{
			if (base.IsDisposed)
			{
				return;
			}
			try
			{
				WritingValueCount++;
				TValue valueToSave = Value;
				await WriteValueAsync(PidComponent, valueToSave, cancellationToken);
				UpdateValue(valueToSave, valueLoaded: true, valueValid: true);
			}
			catch (LogicalDevicePidInvalidValueException)
			{
				UpdateValue(ValueToUseWhenInvalidData, valueLoaded: true, valueValid: false);
				throw;
			}
			finally
			{
				if (WritingValueCount != 0)
				{
					WritingValueCount--;
				}
			}
		}

		private void UpdateValue(TValue value, bool valueLoaded, bool valueValid)
		{
			TValue value2 = value;
			MainThread.RequestMainThreadAction(delegate
			{
				if (!base.IsDisposed)
				{
					if (!base.HasValueBeenLoaded || !valueValid)
					{
						string text = (valueValid ? "" : " INVALID");
						TaggedLog.Debug("LogicalDevicePidBindableAsyncGeneric", $"PID {PidComponent.PropertyId.Name} value is {value2} (valueLoaded = {valueLoaded}){text}");
					}
					if (valueValid)
					{
						base.LastValue = value2;
						Value = value2;
					}
					base.HasValueBeenLoaded = valueLoaded;
					base.IsValueInvalid = !valueValid;
				}
			});
		}

		public override void Dispose(bool isDisposing)
		{
			if (_autoPropertyChanged != null)
			{
				try
				{
					base.PropertyChanged -= _autoPropertyChanged;
				}
				catch
				{
				}
				_autoPropertyChanged = null;
			}
			_initialPidRefreshCancelTokenSource?.TryCancelAndDispose();
			_initialPidRefreshCancelTokenSource = null;
			_autoSaveCancelTokenSource?.TryCancelAndDispose();
			_autoSaveCancelTokenSource = null;
			_autoRefreshTimer?.TryCancelAndDispose();
			_autoRefreshTimer = null;
			base.Dispose(isDisposing);
		}
	}
}
