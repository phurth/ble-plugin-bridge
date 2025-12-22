using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimBool : LogicalDevicePidSim, ILogicalDevicePidBool, ILogicalDevicePidProperty<bool>, ILogicalDevicePid<bool>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public bool ValueBool
		{
			get
			{
				return !base.ValueRaw.Equals(UInt48.Zero);
			}
			set
			{
				base.ValueRaw = (value ? ((UInt48)1) : UInt48.Zero);
			}
		}

		public LogicalDevicePidSimBool(PID pid, bool value = false)
			: base(pid, (ulong)(value ? 1 : 0))
		{
		}

		public Task<bool> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadBoolAsync(cancellationToken);
		}

		public Task WriteAsync(bool value, CancellationToken cancellationToken)
		{
			return WriteBoolAsync(value, cancellationToken);
		}

		public async Task<bool> ReadBoolAsync(CancellationToken cancellationToken)
		{
			return await ReadValueAsync(cancellationToken) != 0;
		}

		public Task WriteBoolAsync(bool value, CancellationToken cancellationToken)
		{
			return WriteValueAsync((ulong)(value ? 1 : 0), cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueBool");
		}
	}
}
