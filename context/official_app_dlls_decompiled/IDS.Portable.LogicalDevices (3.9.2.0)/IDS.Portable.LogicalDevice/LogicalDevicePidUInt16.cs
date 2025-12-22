using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidUInt16 : LogicalDevicePid, ILogicalDevicePidUInt16, ILogicalDevicePidProperty<ushort>, ILogicalDevicePid<ushort>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public delegate ushort ValueToUInt16(ulong value);

		public delegate ulong ValueFromUInt16(ushort value);

		protected readonly ValueToUInt16 ConvertToUInt16;

		protected readonly ValueFromUInt16 ConvertFromUInt16;

		public ushort ValueUInt16
		{
			get
			{
				return ConvertToUInt16(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)ConvertFromUInt16(value);
			}
		}

		public LogicalDevicePidUInt16(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess)
			: this(logicalDevice, pid, writeAccess, null, null)
		{
		}

		public LogicalDevicePidUInt16(ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ValueToUInt16? convertToUInt16, ValueFromUInt16? convertFromUInt16, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
			ConvertToUInt16 = convertToUInt16 ?? ((ValueToUInt16)((ulong longValue) => (ushort)longValue));
			ConvertFromUInt16 = convertFromUInt16 ?? ((ValueFromUInt16)((ushort uInt16Value) => uInt16Value));
		}

		public Task<ushort> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadUInt16Async(cancellationToken);
		}

		public Task WriteAsync(ushort value, CancellationToken cancellationToken)
		{
			return WriteUInt16Async(value, cancellationToken);
		}

		public async Task<ushort> ReadUInt16Async(CancellationToken cancellationToken)
		{
			ulong value = await ReadValueAsync(cancellationToken);
			return ConvertToUInt16(value);
		}

		public Task WriteUInt16Async(ushort value, CancellationToken cancellationToken)
		{
			ulong value2 = ConvertFromUInt16(value);
			return WriteValueAsync(value2, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueUInt16");
		}
	}
}
