using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidSim : CommonDisposableNotifyPropertyChanged, ILogicalDevicePidSim, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		private ulong _value;

		private readonly Action<ulong>? _onChangedHandler;

		public int PidReadTimeoutSec { get; set; } = 3;


		public int PidWriteTimeoutSec { get; set; } = 6;


		public PID PropertyId { get; protected set; }

		public AsyncValueCachedState ValueState => AsyncValueCachedState.HasValue;

		public UInt48 ValueRaw
		{
			get
			{
				return (UInt48)_value;
			}
			set
			{
				if (!value.Equals((UInt48)_value))
				{
					_value = value;
					_onChangedHandler?.Invoke(_value);
					NotifyPropertyChanged("ValueRaw");
					ValueRawPropertyChanged();
				}
			}
		}

		public bool IsReadOnly { get; }

		public virtual IPidDetail PidDetail => PropertyId.ConvertToPid().GetPidDetailDefault();

		public LogicalDevicePidSim(PID pid, ulong value = 0uL, bool isReadOnly = false)
		{
			IsReadOnly = isReadOnly;
			PropertyId = pid;
			_value = value;
			_onChangedHandler = null;
		}

		public LogicalDevicePidSim(PID pid, ulong value, Action<ulong> onChanged)
			: this(pid, value)
		{
			_onChangedHandler = onChanged;
		}

		public Task<ulong> ReadValueAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(_value);
		}

		public Task WriteValueAsync(ulong value, CancellationToken cancellationToken)
		{
			if (IsReadOnly)
			{
				throw new LogicalDevicePidValueWriteNotSupportedException(PropertyId);
			}
			ValueRaw = (UInt48)value;
			return Task.CompletedTask;
		}

		public override string ToString()
		{
			return $"PID SIM {PropertyId}";
		}

		protected virtual void ValueRawPropertyChanged()
		{
		}
	}
}
