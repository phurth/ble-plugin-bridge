using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidUInt48 : LogicalDevicePid, ILogicalDevicePidUInt48, ILogicalDevicePidProperty<UInt48>, ILogicalDevicePid<UInt48>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public UInt48 ValueUInt48
		{
			get
			{
				return base.ValueRaw;
			}
			set
			{
				base.ValueRaw = value;
			}
		}

		public LogicalDevicePidUInt48(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
		}

		public async Task<UInt48> ReadAsync(CancellationToken cancellationToken)
		{
			return (UInt48)(await ReadValueAsync(cancellationToken));
		}

		public Task WriteAsync(UInt48 value, CancellationToken cancellationToken)
		{
			return WriteValueAsync(value, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueUInt48");
		}
	}
}
