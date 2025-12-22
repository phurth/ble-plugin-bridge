using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidMap<TValue> : LogicalDevicePid, ILogicalDevicePidMap<TValue>, ILogicalDevicePidProperty<TValue>, ILogicalDevicePid<TValue>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		public Func<ulong, TValue> MapToValue { get; }

		public TValue ValueMap
		{
			get
			{
				return MapToValue(base.ValueRaw);
			}
			set
			{
				throw new LogicalDevicePidMappingNotSupportedException(base.PropertyId, "Mapping from TValue not yet supported by LogicalDevicePidMap");
			}
		}

		public LogicalDevicePidMap(Func<ulong, TValue> mapToValue, ILogicalDevice logicalDevice, PID pid, LogicalDeviceSessionType writeAccess, Func<ulong, bool>? validityCheck = null)
			: base(logicalDevice, pid, writeAccess, validityCheck)
		{
			MapToValue = mapToValue;
		}

		public Task<TValue> ReadAsync(CancellationToken cancellationToken)
		{
			return ReadMapAsync(cancellationToken);
		}

		public Task WriteAsync(TValue value, CancellationToken cancellationToken)
		{
			return WriteMapAsync(value, cancellationToken);
		}

		public async Task<TValue> ReadMapAsync(CancellationToken cancellationToken)
		{
			if (MapToValue == null)
			{
				throw new LogicalDevicePidMappingNotSupportedException(base.PropertyId, "MapToValue is null so unable to map value");
			}
			ulong arg = await ReadValueAsync(cancellationToken);
			return MapToValue(arg);
		}

		public Task WriteMapAsync(TValue value, CancellationToken cancellationToken)
		{
			throw new LogicalDevicePidMappingNotSupportedException(base.PropertyId, "Mapping from TValue not yet supported by LogicalDevicePidMap");
		}

		protected override void ValueRawPropertyChanged()
		{
			base.ValueRawPropertyChanged();
			OnPropertyChanged("ValueMap");
		}
	}
}
