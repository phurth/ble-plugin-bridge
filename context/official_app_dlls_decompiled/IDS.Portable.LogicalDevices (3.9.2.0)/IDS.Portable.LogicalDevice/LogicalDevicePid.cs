using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.FirmwareUpdate;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePid : CommonDisposableNotifyPropertyChanged, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		private const string LogTag = "LogicalDevicePid";

		public const int DefaultReadTimeoutSec = 3;

		public const int DefaultReadListTimeoutSec = 10;

		public const int DefaultWriteTimeoutSec = 6;

		public const int DefaultQueryCompletePollInterval = 100;

		public const int DefaultPidCacheTimeoutMs = 250;

		public const int PropertyWriteDebounceTimeMs = 250;

		public readonly ILogicalDevice LogicalDevice;

		protected readonly LogicalDeviceSessionType WriteAccess;

		protected readonly Func<ulong, bool>? ValidityCheckRead;

		protected readonly Func<ulong, bool>? ValidityCheckWrite;

		private readonly AsyncValueCached<ulong> _valueCache;

		private readonly AsyncValueBatchedReader<ulong> _valueBatchedReader;

		private CancellationTokenSource? _valueRawCancellationTokenSource;

		public int PidReadTimeoutSec { get; } = 3;


		public int PidWriteTimeoutSec { get; } = 6;


		public int PidQueryCompletePollInterval { get; } = 100;


		public int PidCacheTimeoutMs { get; } = 250;


		public PID PropertyId { get; }

		protected ushort? RawPidAddress { get; }

		public bool IsReadOnly => WriteAccess == LogicalDeviceSessionType.None;

		public virtual IPidDetail PidDetail => LogicalDevice?.GetPidDetail(PropertyId.ConvertToPid()) ?? PropertyId.ConvertToPid().GetPidDetailDefault();

		public UInt48 ValueRaw
		{
			get
			{
				(ulong Value, AsyncValueCachedState State) valueAndState = _valueCache.ValueAndState;
				var (num, _) = valueAndState;
				switch (valueAndState.State)
				{
				case AsyncValueCachedState.NoValue:
				{
					Pid pid = PropertyId.ConvertToPid();
					if (pid.IsAutoCacheingPid())
					{
						UInt48? cachedPidRawValue = LogicalDevice.GetCachedPidRawValue(pid);
						if (cachedPidRawValue.HasValue)
						{
							UInt48 valueOrDefault = cachedPidRawValue.GetValueOrDefault();
							_valueCache.Value = valueOrDefault;
							_valueCache.InvalidateCache();
							num = valueOrDefault;
						}
					}
					ReadValueAsync(CancellationToken.None);
					break;
				}
				case AsyncValueCachedState.HasValueNeedsUpdate:
					ReadValueAsync(CancellationToken.None);
					break;
				}
				return (UInt48)num;
			}
			set
			{
				if (base.IsDisposed || LogicalDevice.IsDisposed)
				{
					TaggedLog.Warning("LogicalDevicePid", string.Format("PID {0} write ignored as object is disposed {1} for {2}", "ValueRaw", PropertyId, LogicalDevice));
				}
				else
				{
					if (value.Equals((UInt48)_valueCache.Value))
					{
						return;
					}
					_valueRawCancellationTokenSource?.TryCancelAndDispose();
					CancellationTokenSource valueRawCancellationTokenSource = (_valueRawCancellationTokenSource = new CancellationTokenSource());
					Task.Run(async delegate
					{
						_ = 1;
						try
						{
							await Task.Delay(250, valueRawCancellationTokenSource.Token);
							if (!valueRawCancellationTokenSource.Token.IsCancellationRequested)
							{
								await WriteValueAsync(value, valueRawCancellationTokenSource.Token);
							}
						}
						catch (OperationCanceledException) when (valueRawCancellationTokenSource.Token.IsCancellationRequested)
						{
						}
						catch (Exception)
						{
							TaggedLog.Warning("LogicalDevicePid", string.Format("PID {0} write failed sposed {1} for {2}", "ValueRaw", PropertyId, LogicalDevice));
						}
					});
				}
			}
		}

		public AsyncValueCachedState ValueState => _valueCache.ValueAndState.State;

		public LogicalDevicePid(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, Func<ulong, bool>? validityCheckRead, Func<ulong, bool>? validityCheckWrite)
		{
			LogicalDevice = logicalDevice ?? throw new ArgumentNullException("logicalDevice");
			PropertyId = pid;
			WriteAccess = writeAccess;
			ValidityCheckRead = validityCheckRead;
			ValidityCheckWrite = validityCheckWrite;
			RawPidAddress = null;
			_valueCache = new AsyncValueCached<ulong>(PidCacheTimeoutMs);
			_valueCache.PropertyChanged += ValueRawPropertyChanged;
			_valueBatchedReader = new AsyncValueBatchedReader<ulong>(ReadValueImplAsync, _valueCache, PidReadTimeoutSec * 1000);
		}

		public LogicalDevicePid(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, Func<ulong, bool>? validityCheck = null)
			: this(logicalDevice, pid, writeAccess, validityCheck, validityCheck)
		{
		}

		protected LogicalDevicePid(ILogicalDevice logicalDevice, PID pid, ushort rawPidAddress, LogicalDeviceSessionType writeAccess, Func<ulong, bool>? validityCheck = null)
			: this(logicalDevice, pid, writeAccess, validityCheck)
		{
			RawPidAddress = rawPidAddress;
		}

		protected virtual void ReadProgress(float percentComplete, string status)
		{
		}

		protected virtual void WriteProgress(float percentComplete, string status)
		{
		}

		public Task<ulong> ReadValueAsync(CancellationToken cancellationToken)
		{
			return _valueBatchedReader.ReadValueAsync(cancellationToken);
		}

		public Task<ulong> ReadValueAsync(CancellationToken cancellationToken, bool forceUpdate)
		{
			return _valueBatchedReader.ReadValueAsync(cancellationToken, forceUpdate);
		}

		public virtual async Task<ulong> ReadValueImplAsync(CancellationToken cancellationToken)
		{
			CancelPidOperationIfNotAllowed(LogicalDevice);
			ILogicalDeviceSourceDirectPid logicalDeviceSourceDirectPid = LogicalDevice.DeviceService.DeviceSourceManager.GetPrimaryDeviceSource<ILogicalDeviceSourceDirectPid>(LogicalDevice) ?? throw new LogicalDevicePidValueReadNotSupportedException(PropertyId, RawPidAddress, LogicalDevice);
			Pid pid = PropertyId.ConvertToPid();
			if (pid == Pid.Unknown)
			{
				throw new LogicalDevicePidException(PropertyId, "No match found converting PID!");
			}
			ulong num = (RawPidAddress.HasValue ? (await logicalDeviceSourceDirectPid.PidReadAsync(LogicalDevice, pid, RawPidAddress.Value, ReadProgress, cancellationToken)) : ((ulong)(await logicalDeviceSourceDirectPid.PidReadAsync(LogicalDevice, pid, ReadProgress, cancellationToken))));
			if (!(ValidityCheckRead?.Invoke(num) ?? true))
			{
				throw new LogicalDevicePidInvalidValueException(PropertyId, RawPidAddress, LogicalDevice, num);
			}
			return num;
		}

		public virtual async Task WriteValueAsync(ulong value, CancellationToken cancellationToken)
		{
			AsyncValueCachedOperation<ulong> asyncValueCachedOperation = null;
			try
			{
				CancelPidOperationIfNotAllowed(LogicalDevice);
				if (!(ValidityCheckWrite?.Invoke(value) ?? true))
				{
					throw new LogicalDevicePidInvalidValueException(PropertyId, RawPidAddress, LogicalDevice, value);
				}
				asyncValueCachedOperation = _valueCache.AsyncUpdateStart(value);
				ILogicalDeviceSourceDirectPid logicalDeviceSourceDirectPid = LogicalDevice.DeviceService.DeviceSourceManager.GetPrimaryDeviceSource<ILogicalDeviceSourceDirectPid>(LogicalDevice) ?? throw new LogicalDevicePidValueWriteNotSupportedException(PropertyId, RawPidAddress, LogicalDevice);
				if (RawPidAddress.HasValue)
				{
					await logicalDeviceSourceDirectPid.PidWriteAsync(LogicalDevice, PropertyId.ConvertToPid(), RawPidAddress.Value, (uint)value, WriteAccess, WriteProgress, cancellationToken);
				}
				else
				{
					await logicalDeviceSourceDirectPid.PidWriteAsync(LogicalDevice, PropertyId.ConvertToPid(), (UInt48)value, WriteAccess, WriteProgress, cancellationToken);
				}
				IAsyncValueCached<ulong> readCachedValue = _valueBatchedReader.ReadCachedValue;
				if (readCachedValue != null && readCachedValue.HasValue && value != readCachedValue.Value)
				{
					readCachedValue.InvalidateCache();
				}
				_valueCache.AsyncUpdateComplete(asyncValueCachedOperation);
			}
			catch (LogicalDevicePidValueWriteException ex)
			{
				TaggedLog.Error("LogicalDevicePid", ex.Message ?? "");
				_valueCache.AsyncUpdateFailed(asyncValueCachedOperation);
				throw;
			}
			catch (Exception ex2)
			{
				TaggedLog.Error("LogicalDevicePid", $"WriteValueAsync unable to WRITE PID {PropertyId} with value {value}: {ex2.Message}");
				_valueCache.AsyncUpdateFailed(asyncValueCachedOperation);
				throw;
			}
		}

		public static void CancelPidOperationIfNotAllowed(ILogicalDevice logicalDevice)
		{
			if ((byte)logicalDevice.LogicalId.DeviceType != 50)
			{
				ILogicalDeviceFirmwareUpdateDevice startedSessionFirmwareUpdateDevice = logicalDevice.DeviceService.FirmwareUpdateManager.GetStartedSessionFirmwareUpdateDevice();
				if (startedSessionFirmwareUpdateDevice != null && startedSessionFirmwareUpdateDevice != logicalDevice)
				{
					throw new LogicalDevicePidOperationCanceledBecauseFirmwareUpdateInProgress();
				}
			}
		}

		public override string ToString()
		{
			if (RawPidAddress.HasValue)
			{
				return $"PID {PropertyId}[{RawPidAddress}] {LogicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.FunctionNameFullWithoutFunctionInstance)}";
			}
			return $"PID {PropertyId} {LogicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.FunctionNameFullWithoutFunctionInstance)}";
		}

		private void ValueRawPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			string propertyName = e.PropertyName;
			if (!(propertyName == "Value"))
			{
				if (propertyName == "State")
				{
					OnPropertyChanged("ValueState");
				}
			}
			else
			{
				OnPropertyChanged("ValueRaw");
				ValueRawPropertyChanged();
			}
		}

		protected virtual void ValueRawPropertyChanged()
		{
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_valueRawCancellationTokenSource?.TryCancelAndDispose();
		}
	}
}
