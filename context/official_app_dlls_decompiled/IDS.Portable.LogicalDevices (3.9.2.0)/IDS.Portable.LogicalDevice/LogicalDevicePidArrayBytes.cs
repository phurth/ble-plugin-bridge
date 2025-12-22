using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidArrayBytes : LogicalDevicePid, ILogicalDevicePidArrayBytes, ILogicalDevicePidProperty<byte[]>, ILogicalDevicePid<byte[]>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public const int ByteMask = 255;

		public ILogicalDevicePidArrayBytes.ValueToArrayBytes ConvertToBytes { get; }

		public ILogicalDevicePidArrayBytes.ValueFromArrayBytes ConvertFromBytes { get; }

		public int NumBytes { get; }

		public byte[] ValueBytes
		{
			get
			{
				return ConvertToBytes(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)ConvertFromBytes(value);
			}
		}

		public LogicalDevicePidArrayBytes(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, int numBytes = 6)
			: this(logicalDevice, pid, writeAccess, numBytes, null, null)
		{
		}

		public LogicalDevicePidArrayBytes(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, int numBytes, ILogicalDevicePidArrayBytes.ValueToArrayBytes? convertToArrayBytes, ILogicalDevicePidArrayBytes.ValueFromArrayBytes? convertFromArrayBytes, Func<ulong, bool>? validityCheck = null)
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
			ConvertToBytes = convertToArrayBytes ?? new ILogicalDevicePidArrayBytes.ValueToArrayBytes(DefaultConvertToArrayLeastSignificantByteFirst);
			ConvertFromBytes = convertFromArrayBytes ?? new ILogicalDevicePidArrayBytes.ValueFromArrayBytes(DefaultConvertFromArrayLeastSignificantByteFirst);
		}

		public byte[] DefaultConvertToArrayMostSignificantByteFirst(ulong value)
		{
			byte[] array = new byte[NumBytes];
			if (NumBytes == 0)
			{
				return array;
			}
			for (int num = NumBytes - 1; num >= 0; num--)
			{
				array[num] = (byte)(value & 0xFF);
				value >>= 8;
			}
			return array;
		}

		public ulong DefaultConvertFromArrayMostSignificantByteFirst(byte[] buffer)
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
				num |= buffer[i];
			}
			return num;
		}

		public byte[] DefaultConvertToArrayLeastSignificantByteFirst(ulong value)
		{
			byte[] array = new byte[NumBytes];
			if (NumBytes == 0)
			{
				return array;
			}
			for (int i = 0; i < NumBytes; i++)
			{
				array[i] = (byte)(value & 0xFF);
				value >>= 8;
			}
			return array;
		}

		public ulong DefaultConvertFromArrayLeastSignificantByteFirst(byte[] buffer)
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
				num |= buffer[num2];
			}
			return num;
		}

		public Task<byte[]> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadArrayByteAsync(cancellationToken);
		}

		public Task WriteAsync(byte[] value, CancellationToken cancellationToken)
		{
			return WriteArrayByteAsync(value, cancellationToken);
		}

		public async Task<byte[]> ReadArrayByteAsync(CancellationToken cancellationToken)
		{
			ulong value = await ReadValueAsync(cancellationToken);
			return ConvertToBytes(value);
		}

		public Task WriteArrayByteAsync(byte[] value, CancellationToken cancellationToken)
		{
			ulong value2 = ConvertFromBytes(value);
			return WriteValueAsync(value2, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueBytes");
		}
	}
}
