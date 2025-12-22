using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimByte : LogicalDevicePidSimOfType<byte>, ILogicalDevicePidByte, ILogicalDevicePidProperty<byte>, ILogicalDevicePid<byte>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public byte ValueByte
		{
			get
			{
				return ReadConverter(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = WriteConverter(value);
			}
		}

		public LogicalDevicePidSimByte(PID pid, byte value)
			: base(pid, (Func<UInt48, byte>)ReadConverter, (Func<byte, UInt48>)WriteConverter, value)
		{
		}

		public static byte ReadConverter(UInt48 rawValue)
		{
			return (byte)rawValue;
		}

		public static UInt48 WriteConverter(byte value)
		{
			return value;
		}

		public Task<byte> ReadByteAsync(CancellationToken cancellationToken)
		{
			return ReadAsync(cancellationToken);
		}

		public Task WriteByteAsync(byte value, CancellationToken cancellationToken)
		{
			return WriteAsync(value, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueByte");
		}
	}
}
