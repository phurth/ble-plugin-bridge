using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidFixedPointBool : LogicalDevicePidFixedPoint, ILogicalDevicePidFixedPointBool, ILogicalDevicePidFixedPoint, ILogicalDevicePidProperty<float>, ILogicalDevicePid<float>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged, ILogicalDevicePidBool, ILogicalDevicePidProperty<bool>, ILogicalDevicePid<bool>
	{
		public delegate bool ValueToBool(FixedPointType fixedPointConversion, ulong value);

		public delegate ulong ValueFromBool(FixedPointType fixedPointConversion, bool value);

		protected readonly ValueToBool ConvertToBool;

		protected readonly ValueFromBool ConvertFromBool;

		public bool ValueBool
		{
			get
			{
				return ConvertToBool(base.FixedPointConversion, base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)ConvertFromBool(base.FixedPointConversion, value);
			}
		}

		public LogicalDevicePidFixedPointBool(FixedPointType fixedPointConversion, ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, ValueToBool? convertToBool = null, ValueFromBool? convertFromBool = null, Func<ulong, bool>? validityCheck = null)
			: base(fixedPointConversion, logicalDevice, pid, writeAccess, validityCheck)
		{
			ConvertToBool = convertToBool ?? ((ValueToBool)((FixedPointType fpConversion, ulong longValue) => fixedPointConversion.FixedPointToFloat(longValue) != 0f));
			ConvertFromBool = convertFromBool ?? ((ValueFromBool)((FixedPointType fpConversion, bool boolValue) => (!boolValue) ? 0 : fixedPointConversion.ToFixedPointAsULong(1f)));
		}

		public new Task<bool> ReadAsync(CancellationToken cancellationToken)
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
			return ConvertToBool(base.FixedPointConversion, value);
		}

		public Task WriteBoolAsync(bool value, CancellationToken cancellationToken)
		{
			ulong value2 = ConvertFromBool(base.FixedPointConversion, value);
			return WriteValueAsync(value2, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueBool");
		}
	}
}
