using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimUInt48 : LogicalDevicePidSimOfType<UInt48>, ILogicalDevicePidUInt48, ILogicalDevicePidProperty<UInt48>, ILogicalDevicePid<UInt48>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public UInt48 ValueUInt48
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

		public LogicalDevicePidSimUInt48(PID pid, UInt48 value)
			: base(pid, (Func<UInt48, UInt48>)ReadConverter, (Func<UInt48, UInt48>)WriteConverter, value)
		{
		}

		public static UInt48 ReadConverter(UInt48 rawValue)
		{
			return rawValue;
		}

		public static UInt48 WriteConverter(UInt48 value)
		{
			return value;
		}

		public Task<UInt48> ReadUInt48Async(CancellationToken cancellationToken)
		{
			return ReadAsync(cancellationToken);
		}

		public Task WriteUInt48Async(UInt48 value, CancellationToken cancellationToken)
		{
			return WriteAsync(value, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueUInt48");
		}
	}
}
