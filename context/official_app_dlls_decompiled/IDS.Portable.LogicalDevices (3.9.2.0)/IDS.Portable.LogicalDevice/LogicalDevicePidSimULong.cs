using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimULong : LogicalDevicePidSim, ILogicalDevicePidULong, ILogicalDevicePidProperty<ulong>, ILogicalDevicePid<ulong>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public ulong ValueULong
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

		public LogicalDevicePidSimULong(PID pid, ulong value = 0uL)
			: base(pid, value)
		{
		}

		public LogicalDevicePidSimULong(PID pid, ulong value, Action<ulong> onChanged)
			: base(pid, value, onChanged)
		{
		}

		public static ulong ReadConverter(UInt48 rawValue)
		{
			return rawValue;
		}

		public static UInt48 WriteConverter(ulong value)
		{
			return (UInt48)value;
		}

		public Task<ulong> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadValueAsync(cancellationToken);
		}

		public Task WriteAsync(ulong value, CancellationToken cancellationToken)
		{
			return WriteValueAsync(value, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueULong");
		}
	}
}
