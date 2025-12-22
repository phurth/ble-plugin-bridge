using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidULong : LogicalDevicePid, ILogicalDevicePidULong, ILogicalDevicePidProperty<ulong>, ILogicalDevicePid<ulong>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public ulong BitMask;

		public ulong ValueULong
		{
			get
			{
				return base.ValueRaw;
			}
			set
			{
				base.ValueRaw = (UInt48)value;
			}
		}

		public LogicalDevicePidULong(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, Func<ulong, bool>? validityCheck = null, ulong bitMask = 72057594037927935uL)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
			BitMask = bitMask & (ulong)UInt48.MaxValue;
		}

		public async Task<ulong> ReadAsync(CancellationToken cancellationToken)
		{
			return await ReadValueAsync(cancellationToken) & BitMask;
		}

		public Task WriteAsync(ulong value, CancellationToken cancellationToken)
		{
			return WriteValueAsync(value & BitMask, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueULong");
		}
	}
}
