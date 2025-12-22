using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidArrayByteSim : LogicalDevicePidSim, ILogicalDevicePidArrayBytes, ILogicalDevicePidProperty<byte[]>, ILogicalDevicePid<byte[]>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public const int ByteMask = 255;

		public int NumBytes { get; }

		public ILogicalDevicePidArrayBytes.ValueToArrayBytes ConvertToBytes { get; }

		public ILogicalDevicePidArrayBytes.ValueFromArrayBytes ConvertFromBytes { get; }

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

		public LogicalDevicePidArrayByteSim(PID pid, ulong value = 0uL, int numBytes = 6, bool isReadOnly = false)
			: base(pid, value, isReadOnly)
		{
			NumBytes = numBytes;
			ConvertToBytes = DefaultConvertToArrayLeastSignificantByteFirst;
			ConvertFromBytes = DefaultConvertFromArrayLeastSignificantByteFirst;
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
			ILogicalDevicePidArrayBytes.ValueToArrayBytes convertToBytes = ConvertToBytes;
			return convertToBytes(await ReadValueAsync(cancellationToken));
		}

		public async Task WriteArrayByteAsync(byte[] value, CancellationToken cancellationToken)
		{
			await WriteValueAsync(ConvertFromBytes(value), cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueBytes");
		}
	}
}
