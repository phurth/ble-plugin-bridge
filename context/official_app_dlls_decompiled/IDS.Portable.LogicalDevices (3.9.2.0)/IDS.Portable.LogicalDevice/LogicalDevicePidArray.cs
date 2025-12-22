using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidArray<TValue> : ILogicalDevicePidArray<TValue>
	{
		private const string LogTag = "LogicalDevicePidArray";

		public readonly ILogicalDevice LogicalDevice;

		protected readonly Func<ulong, bool>? ValidityCheckRead;

		protected readonly Func<ulong, bool>? ValidityCheckWrite;

		public Func<TValue, ulong> FromValueConverter;

		public Func<ulong, TValue> ToValueConverter;

		public PID PropertyId { get; }

		public LogicalDeviceSessionType WriteAccess { get; }

		public bool IsReadOnly => WriteAccess == LogicalDeviceSessionType.None;

		public ushort MinIndex { get; }

		public ushort MaxIndex { get; }

		public virtual IPidDetail PidDetail => LogicalDevice?.GetPidDetail(PropertyId.ConvertToPid()) ?? PropertyId.ConvertToPid().GetPidDetailDefault();

		public LogicalDevicePidArray(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ushort minIndex, ushort maxIndex, Func<TValue, ulong> fromValue, Func<ulong, TValue> toValue, Func<ulong, bool>? validityCheckRead, Func<ulong, bool>? validityCheckWrite)
		{
			if (minIndex > maxIndex)
			{
				throw new ArgumentOutOfRangeException("maxIndex");
			}
			LogicalDevice = logicalDevice ?? throw new ArgumentNullException("logicalDevice");
			PropertyId = pid;
			WriteAccess = writeAccess;
			MinIndex = minIndex;
			MaxIndex = maxIndex;
			FromValueConverter = fromValue;
			ToValueConverter = toValue;
			ValidityCheckRead = validityCheckRead;
			ValidityCheckWrite = validityCheckWrite;
		}

		public LogicalDevicePidArray(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ushort minIndex, ushort maxIndex, Func<TValue, ulong> fromValue, Func<ulong, TValue> toValue, Func<ulong, bool>? validityCheck = null)
			: this(logicalDevice, pid, writeAccess, minIndex, maxIndex, fromValue, toValue, validityCheck, validityCheck)
		{
		}

		protected virtual void ReadProgress(float percentComplete, string status)
		{
		}

		protected virtual void WriteProgress(float percentComplete, string status)
		{
		}

		public virtual async Task<TValue> ReadValueAsync(ushort index, CancellationToken cancellationToken)
		{
			ulong arg = await ReadAsync(index, cancellationToken);
			return ToValueConverter(arg);
		}

		public virtual Task WriteValueAsync(ushort index, TValue value, CancellationToken cancellationToken)
		{
			return WriteAsync(index, FromValueConverter(value), cancellationToken);
		}

		protected virtual async Task<ulong> ReadAsync(ushort index, CancellationToken cancellationToken)
		{
			if (index < MinIndex || index > MaxIndex)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			LogicalDevicePid.CancelPidOperationIfNotAllowed(LogicalDevice);
			ILogicalDeviceSourceDirectPid? obj = LogicalDevice.DeviceService.DeviceSourceManager.GetPrimaryDeviceSource<ILogicalDeviceSourceDirectPid>(LogicalDevice) ?? throw new LogicalDevicePidValueReadNotSupportedException(PropertyId, index, LogicalDevice);
			Pid pid = PropertyId.ConvertToPid();
			if (pid == Pid.Unknown)
			{
				throw new LogicalDevicePidException(PropertyId, "No match found converting PID!");
			}
			ulong num = await obj!.PidReadAsync(LogicalDevice, pid, index, ReadProgress, cancellationToken);
			if (!(ValidityCheckRead?.Invoke(num) ?? true))
			{
				throw new LogicalDevicePidInvalidValueException(PropertyId, index, LogicalDevice, num);
			}
			return num;
		}

		protected virtual async Task WriteAsync(ushort index, ulong value, CancellationToken cancellationToken)
		{
			try
			{
				if (index < MinIndex || index > MaxIndex)
				{
					throw new ArgumentOutOfRangeException("index");
				}
				LogicalDevicePid.CancelPidOperationIfNotAllowed(LogicalDevice);
				if (!(ValidityCheckWrite?.Invoke(value) ?? true))
				{
					throw new LogicalDevicePidInvalidValueException(PropertyId, index, LogicalDevice, value);
				}
				await (LogicalDevice.DeviceService.DeviceSourceManager.GetPrimaryDeviceSource<ILogicalDeviceSourceDirectPid>(LogicalDevice) ?? throw new LogicalDevicePidValueWriteNotSupportedException(PropertyId, index, LogicalDevice))!.PidWriteAsync(LogicalDevice, PropertyId.ConvertToPid(), index, (uint)value, WriteAccess, WriteProgress, cancellationToken);
			}
			catch (LogicalDevicePidValueWriteException ex)
			{
				TaggedLog.Error("LogicalDevicePidArray", ex.Message ?? "");
				throw;
			}
			catch (Exception ex2)
			{
				TaggedLog.Error("LogicalDevicePidArray", $"WriteValueAsync unable to WRITE PID {PropertyId} with value {value}: {ex2.Message}");
				throw;
			}
		}

		public override string ToString()
		{
			return $"PID Array {PropertyId} {LogicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.FunctionNameFullWithoutFunctionInstance)}";
		}
	}
}
