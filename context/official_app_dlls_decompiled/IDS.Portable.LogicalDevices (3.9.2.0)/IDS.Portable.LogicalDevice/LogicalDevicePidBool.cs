using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidBool : LogicalDevicePid, ILogicalDevicePidBool, ILogicalDevicePidProperty<bool>, ILogicalDevicePid<bool>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public delegate bool ValueToBool(ulong value);

		public delegate ulong ValueFromBool(bool value);

		protected readonly ValueToBool ConvertToBool;

		protected readonly ValueFromBool ConvertFromBool;

		public bool ValueBool
		{
			get
			{
				return ConvertToBool(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)ConvertFromBool(value);
			}
		}

		public LogicalDevicePidBool(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess)
			: this(logicalDevice, pid, writeAccess, null, null)
		{
		}

		public LogicalDevicePidBool(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ValueToBool? convertToBool, ValueFromBool? convertFromBool, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
			ConvertToBool = convertToBool ?? ((ValueToBool)((ulong longValue) => longValue != 0));
			ConvertFromBool = convertFromBool ?? ((ValueFromBool)((bool boolValue) => (ulong)(boolValue ? 1 : 0)));
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
			ulong value = await ReadValueAsync(cancellationToken);
			return ConvertToBool(value);
		}

		public Task WriteBoolAsync(bool value, CancellationToken cancellationToken)
		{
			ulong value2 = ConvertFromBool(value);
			return WriteValueAsync(value2, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueBool");
		}
	}
}
