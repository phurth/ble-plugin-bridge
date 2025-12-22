using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimUInt16 : LogicalDevicePidSimOfType<ushort>, ILogicalDevicePidUInt16, ILogicalDevicePidProperty<ushort>, ILogicalDevicePid<ushort>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public ushort ValueUInt16
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

		public LogicalDevicePidSimUInt16(PID pid, ushort value)
			: base(pid, (Func<UInt48, ushort>)ReadConverter, (Func<ushort, UInt48>)WriteConverter, value)
		{
		}

		public static ushort ReadConverter(UInt48 rawValue)
		{
			return (ushort)rawValue;
		}

		public static UInt48 WriteConverter(ushort value)
		{
			return value;
		}

		public Task<ushort> ReadUInt16Async(CancellationToken cancellationToken)
		{
			return ReadAsync(cancellationToken);
		}

		public Task WriteUInt16Async(ushort value, CancellationToken cancellationToken)
		{
			return WriteAsync(value, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueUInt16");
		}
	}
}
