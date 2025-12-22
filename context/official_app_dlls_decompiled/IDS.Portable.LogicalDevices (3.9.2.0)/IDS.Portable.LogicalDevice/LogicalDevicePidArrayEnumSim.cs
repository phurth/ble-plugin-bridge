using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidArrayEnumSim<TValue> : LogicalDevicePidSim, ILogicalDevicePidArrayEnum<TValue>, ILogicalDevicePidProperty<TValue[]>, ILogicalDevicePid<TValue[]>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged where TValue : struct, Enum, IConvertible
	{
		public const int ByteMask = 255;

		public int NumBytes { get; }

		public ILogicalDevicePidArrayEnum<TValue>.ValueToArrayEnums ConvertToEnums { get; }

		public ILogicalDevicePidArrayEnum<TValue>.ValueFromArrayEnums ConvertFromEnums { get; }

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

		public LogicalDevicePidArrayEnumSim(PID pid, ulong value = 0uL, int numBytes = 6, bool isReadOnly = false)
			: base(pid, value, isReadOnly)
		{
			NumBytes = numBytes;
			ConvertToEnums = DefaultConvertToArrayLeastSignificantByteFirst;
			ConvertFromEnums = DefaultConvertFromArrayLeastSignificantByteFirst;
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
			ILogicalDevicePidArrayEnum<TValue>.ValueToArrayEnums convertToEnums = ConvertToEnums;
			return convertToEnums(await ReadValueAsync(cancellationToken));
		}

		public async Task WriteArrayByteEnumAsync(TValue[] value, CancellationToken cancellationToken)
		{
			await WriteValueAsync(ConvertFromEnums(value), cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueByteEnums");
		}
	}
}
