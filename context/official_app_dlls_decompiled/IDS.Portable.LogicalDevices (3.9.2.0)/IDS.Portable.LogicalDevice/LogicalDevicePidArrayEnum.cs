using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidArrayEnum<TValue> : LogicalDevicePid, ILogicalDevicePidArrayEnum<TValue>, ILogicalDevicePidProperty<TValue[]>, ILogicalDevicePid<TValue[]>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged where TValue : struct, Enum, IConvertible
	{
		public const int ByteMask = 255;

		public ILogicalDevicePidArrayEnum<TValue>.ValueToArrayEnums ConvertToEnums { get; }

		public ILogicalDevicePidArrayEnum<TValue>.ValueFromArrayEnums ConvertFromEnums { get; }

		public int NumBytes { get; }

		public TValue[] ValueByteEnums
		{
			get
			{
				return ConvertToEnums(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)ConvertFromEnums(value);
			}
		}

		public LogicalDevicePidArrayEnum(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, int numBytes = 6)
			: this(logicalDevice, pid, writeAccess, numBytes, (ILogicalDevicePidArrayEnum<TValue>.ValueToArrayEnums?)null, (ILogicalDevicePidArrayEnum<TValue>.ValueFromArrayEnums?)null, (Func<ulong, bool>?)null)
		{
		}

		public LogicalDevicePidArrayEnum(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, int numBytes, ILogicalDevicePidArrayEnum<TValue>.ValueToArrayEnums? convertToArrayBytes, ILogicalDevicePidArrayEnum<TValue>.ValueFromArrayEnums? convertFromArrayBytes, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
			if (numBytes < 0)
			{
				throw new ArgumentOutOfRangeException("NumBytes", "Must be >= 0");
			}
			if (numBytes > 6)
			{
				throw new ArgumentOutOfRangeException("NumBytes", "Exceeds maximum buffer size");
			}
			NumBytes = numBytes;
			ConvertToEnums = convertToArrayBytes ?? new ILogicalDevicePidArrayEnum<TValue>.ValueToArrayEnums(DefaultConvertToArrayLeastSignificantByteFirst);
			ConvertFromEnums = convertFromArrayBytes ?? new ILogicalDevicePidArrayEnum<TValue>.ValueFromArrayEnums(DefaultConvertFromArrayLeastSignificantByteFirst);
		}

		public TValue[] DefaultConvertToArrayMostSignificantByteFirst(ulong value)
		{
			TValue[] array = new TValue[NumBytes];
			if (NumBytes == 0)
			{
				return array;
			}
			for (int num = NumBytes - 1; num >= 0; num--)
			{
				array[num] = Enum<TValue>.TryConvert(value & 0xFF);
				value >>= 8;
			}
			return array;
		}

		public ulong DefaultConvertFromArrayMostSignificantByteFirst(TValue[] buffer)
		{
			ulong num = 0uL;
			if (NumBytes == 0)
			{
				return 0uL;
			}
			if (buffer.Length != NumBytes)
			{
				throw new ArgumentOutOfRangeException("buffer", $"Buffer must be {NumBytes} bytes in size");
			}
			for (int i = 0; i < NumBytes; i++)
			{
				num <<= 8;
				num |= Convert.ToUInt64(buffer[i]);
			}
			return num;
		}

		public TValue[] DefaultConvertToArrayLeastSignificantByteFirst(ulong value)
		{
			TValue[] array = new TValue[NumBytes];
			if (NumBytes == 0)
			{
				return array;
			}
			for (int i = 0; i < NumBytes; i++)
			{
				array[i] = Enum<TValue>.TryConvert(value & 0xFF);
				value >>= 8;
			}
			return array;
		}

		public ulong DefaultConvertFromArrayLeastSignificantByteFirst(TValue[] buffer)
		{
			ulong num = 0uL;
			if (NumBytes == 0)
			{
				return 0uL;
			}
			if (buffer.Length != NumBytes)
			{
				throw new ArgumentOutOfRangeException("buffer", $"Buffer must be {NumBytes} bytes in size");
			}
			for (int num2 = NumBytes - 1; num2 >= 0; num2--)
			{
				num <<= 8;
				num |= Convert.ToUInt64(buffer[num2]);
			}
			return num;
		}

		public Task<TValue[]> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadArrayByteEnumAsync(cancellationToken);
		}

		public Task WriteAsync(TValue[] value, CancellationToken cancellationToken)
		{
			return WriteArrayByteEnumAsync(value, cancellationToken);
		}

		public async Task<TValue[]> ReadArrayByteEnumAsync(CancellationToken cancellationToken)
		{
			ulong value = await ReadValueAsync(cancellationToken);
			return ConvertToEnums(value);
		}

		public Task WriteArrayByteEnumAsync(TValue[] value, CancellationToken cancellationToken)
		{
			ulong value2 = ConvertFromEnums(value);
			return WriteValueAsync(value2, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueByteEnums");
		}
	}
}
