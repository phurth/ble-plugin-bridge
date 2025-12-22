using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidStub : CommonDisposableNotifyPropertyChanged, ILogicalDevicePid, ILogicalDevicePidBool, ILogicalDevicePidProperty<bool>, ILogicalDevicePid<bool>, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged, ILogicalDevicePidFixedPointBool, ILogicalDevicePidFixedPoint, ILogicalDevicePidProperty<float>, ILogicalDevicePid<float>, ILogicalDevicePidTimeSpan, ILogicalDevicePidProperty<TimeSpan>, ILogicalDevicePid<TimeSpan>, ILogicalDevicePidULong, ILogicalDevicePidProperty<ulong>, ILogicalDevicePid<ulong>
	{
		private const string LogTag = "LogicalDevicePidStub";

		public ulong Value;

		public string ValueString = "";

		public int PidReadTimeoutSec { get; set; } = 3;


		public int PidWriteTimeoutSec { get; set; } = 6;


		public int PidQueryCompletePollInterval { get; set; } = 100;


		public ILogicalDevice? LogicalDevice { get; }

		public PID PropertyId { get; protected set; }

		public bool IsReadOnly { get; }

		public virtual IPidDetail PidDetail => PropertyId.ConvertToPid().GetPidDetailDefault();

		public ulong ValueULong
		{
			get
			{
				throw new LogicalDevicePidNotSupportedException(PropertyId, LogicalDevice);
			}
			set
			{
				throw new LogicalDevicePidNotSupportedException(PropertyId, LogicalDevice);
			}
		}

		public TimeSpan ValueTimeSpan
		{
			get
			{
				throw new LogicalDevicePidNotSupportedException(PropertyId, LogicalDevice);
			}
			set
			{
				throw new LogicalDevicePidNotSupportedException(PropertyId, LogicalDevice);
			}
		}

		public float ValueFloat
		{
			get
			{
				throw new LogicalDevicePidNotSupportedException(PropertyId, LogicalDevice);
			}
			set
			{
				throw new LogicalDevicePidNotSupportedException(PropertyId, LogicalDevice);
			}
		}

		public bool ValueBool
		{
			get
			{
				return Value != 0;
			}
			set
			{
				throw new LogicalDevicePidValueWriteNotSupportedException(PropertyId);
			}
		}

		public UInt48 ValueRaw
		{
			get
			{
				return (UInt48)Value;
			}
			set
			{
				if (IsReadOnly)
				{
					throw new LogicalDevicePidValueWriteNotSupportedException(PropertyId);
				}
				TaggedLog.Debug("LogicalDevicePidStub", $"Write value of {value} ignored for {PropertyId}.");
			}
		}

		public AsyncValueCachedState ValueState => AsyncValueCachedState.HasValue;

		public LogicalDevicePidStub(ILogicalDevice logicalDevice, PID pid, ulong value = 0uL)
		{
			LogicalDevice = logicalDevice;
			PropertyId = pid;
			Value = value;
		}

		public LogicalDevicePidStub(PID pid, string value, bool isReadOnlyPid = false)
		{
			IsReadOnly = isReadOnlyPid;
			PropertyId = pid;
			ValueString = value;
		}

		public Task<ulong> ReadValueAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(Value);
		}

		public Task WriteValueAsync(ulong value, CancellationToken cancellationToken)
		{
			if (IsReadOnly)
			{
				throw new LogicalDevicePidValueWriteNotSupportedException(PropertyId);
			}
			TaggedLog.Debug("LogicalDevicePidStub", $"Write value of {value} ignored for {PropertyId}.");
			return Task.FromResult(0);
		}

		public Task<ulong> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadValueAsync(cancellationToken);
		}

		public Task WriteAsync(ulong value, CancellationToken cancellationToken)
		{
			return WriteValueAsync(value, cancellationToken);
		}

		public Task<TimeSpan> ReadTimeSpanAsync(CancellationToken cancellationToken)
		{
			throw new LogicalDevicePidNotSupportedException(PropertyId, LogicalDevice);
		}

		public Task WriteTimeSpanAsync(TimeSpan value, CancellationToken cancellationToken)
		{
			throw new LogicalDevicePidNotSupportedException(PropertyId, LogicalDevice);
		}

		Task<TimeSpan> ILogicalDevicePid<TimeSpan>.ReadAsync(CancellationToken cancellationToken)
		{
			return ReadTimeSpanAsync(cancellationToken);
		}

		public Task WriteAsync(TimeSpan value, CancellationToken cancellationToken)
		{
			return WriteTimeSpanAsync(value, cancellationToken);
		}

		public Task<float> ReadFloatAsync(CancellationToken cancellationToken)
		{
			throw new LogicalDevicePidNotSupportedException(PropertyId, LogicalDevice);
		}

		public Task WriteFloatAsync(float value, CancellationToken cancellationToken)
		{
			throw new LogicalDevicePidNotSupportedException(PropertyId, LogicalDevice);
		}

		Task<float> ILogicalDevicePid<float>.ReadAsync(CancellationToken cancellationToken)
		{
			return ReadFloatAsync(cancellationToken);
		}

		public Task WriteAsync(float value, CancellationToken cancellationToken)
		{
			return WriteFloatAsync(value, cancellationToken);
		}

		public async Task<bool> ReadBoolAsync(CancellationToken cancellationToken)
		{
			return await ReadValueAsync(cancellationToken) != 0;
		}

		public async Task WriteBoolAsync(bool value, CancellationToken cancellationToken)
		{
			ulong value2 = (ulong)(value ? 1 : 0);
			await WriteValueAsync(value2, cancellationToken);
		}

		Task<bool> ILogicalDevicePid<bool>.ReadAsync(CancellationToken cancellationToken)
		{
			return ReadBoolAsync(cancellationToken);
		}

		public Task WriteAsync(bool value, CancellationToken cancellationToken)
		{
			return WriteBoolAsync(value, cancellationToken);
		}
	}
}
