using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidByte : LogicalDevicePid, ILogicalDevicePidByte, ILogicalDevicePidProperty<byte>, ILogicalDevicePid<byte>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public delegate byte ValueToByte(ulong value);

		public delegate ulong ValueFromByte(byte value);

		protected readonly ValueToByte ConvertToByte;

		protected readonly ValueFromByte ConvertFromByte;

		public byte ValueByte
		{
			get
			{
				return ConvertToByte(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)ConvertFromByte(value);
			}
		}

		public LogicalDevicePidByte(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess)
			: this(logicalDevice, pid, writeAccess, null, null)
		{
		}

		public LogicalDevicePidByte(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ValueToByte? convertToByte, ValueFromByte? convertFromByte, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
			ConvertToByte = convertToByte ?? ((ValueToByte)((ulong longValue) => (byte)longValue));
			ConvertFromByte = convertFromByte ?? ((ValueFromByte)((byte byteValue) => byteValue));
		}

		public Task<byte> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadByteAsync(cancellationToken);
		}

		public Task WriteAsync(byte value, CancellationToken cancellationToken)
		{
			return WriteByteAsync(value, cancellationToken);
		}

		public async Task<byte> ReadByteAsync(CancellationToken cancellationToken)
		{
			ulong value = await ReadValueAsync(cancellationToken);
			return ConvertToByte(value);
		}

		public Task WriteByteAsync(byte value, CancellationToken cancellationToken)
		{
			ulong value2 = ConvertFromByte(value);
			return WriteValueAsync(value2, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueByte");
		}
	}
}
