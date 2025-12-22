using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSimFixedPoint : LogicalDevicePidSim, ILogicalDevicePidFixedPoint, ILogicalDevicePidProperty<float>, ILogicalDevicePid<float>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		protected FixedPointType FixedPointConversion;

		public float ValueFloat
		{
			get
			{
				return FixedPointConversion.FixedPointToFloat(base.ValueRaw);
			}
			set
			{
				base.ValueRaw = (UInt48)FixedPointConversion.ToFixedPointAsULong(value);
			}
		}

		public LogicalDevicePidSimFixedPoint(FixedPointType fixedPointConversion, PID pid, ulong value = 0uL)
			: base(pid, value)
		{
			FixedPointConversion = fixedPointConversion;
		}

		public LogicalDevicePidSimFixedPoint(FixedPointType fixedPointConversion, PID pid, float value)
			: base(pid, fixedPointConversion.ToFixedPointAsULong(value))
		{
			FixedPointConversion = fixedPointConversion;
		}

		public Task<float> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadFloatAsync(cancellationToken);
		}

		public Task WriteAsync(float value, CancellationToken cancellationToken)
		{
			return WriteFloatAsync(value, cancellationToken);
		}

		public async Task<float> ReadFloatAsync(CancellationToken cancellationToken)
		{
			ulong fixedPointNumber = await ReadValueAsync(cancellationToken);
			return FixedPointConversion.FixedPointToFloat(fixedPointNumber);
		}

		public Task WriteFloatAsync(float value, CancellationToken cancellationToken)
		{
			ulong value2 = FixedPointConversion.ToFixedPointAsULong(value);
			return WriteValueAsync(value2, cancellationToken);
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			NotifyPropertyChanged("ValueFloat");
		}
	}
}
