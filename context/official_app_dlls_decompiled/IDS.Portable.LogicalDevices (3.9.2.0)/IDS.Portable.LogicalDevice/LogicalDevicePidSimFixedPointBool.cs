using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimFixedPointBool : LogicalDevicePidSimFixedPoint, ILogicalDevicePidFixedPointBool, ILogicalDevicePidFixedPoint, ILogicalDevicePidProperty<float>, ILogicalDevicePid<float>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged, ILogicalDevicePidBool, ILogicalDevicePidProperty<bool>, ILogicalDevicePid<bool>
	{
		public bool ValueBool
		{
			get
			{
				return ConvertToBool(FixedPointConversion, base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)ConvertFromBool(FixedPointConversion, value);
			}
		}

		public static bool ConvertToBool(FixedPointType fixedPointConversion, ulong value)
		{
			return fixedPointConversion.FixedPointToFloat(value) != 0f;
		}

		public static ulong ConvertFromBool(FixedPointType fixedPointConversion, bool value)
		{
			if (!value)
			{
				return 0uL;
			}
			return fixedPointConversion.ToFixedPointAsULong(1f);
		}

		public LogicalDevicePidSimFixedPointBool(FixedPointType fixedPointConversion, PID pid, bool value = false)
			: base(fixedPointConversion, pid, ConvertFromBool(fixedPointConversion, value))
		{
		}

		public async Task<bool> ReadBoolAsync(CancellationToken cancellationToken)
		{
			ulong value = await ReadValueAsync(cancellationToken);
			return ConvertToBool(FixedPointConversion, value);
		}

		public Task WriteBoolAsync(bool value, CancellationToken cancellationToken)
		{
			ulong value2 = ConvertFromBool(FixedPointConversion, value);
			return WriteValueAsync(value2, cancellationToken);
		}

		public new Task<bool> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadBoolAsync(cancellationToken);
		}

		public Task WriteAsync(bool value, CancellationToken cancellationToken)
		{
			return WriteBoolAsync(value, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueBool");
		}
	}
}
