using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidProxy : LogicalDevicePidProxy<ILogicalDevicePidProperty>
	{
		public LogicalDevicePidProxy(ILogicalDevicePidProperty? devicePid = null)
			: base(devicePid)
		{
		}
	}
	public class LogicalDevicePidProxy<TLogicalDevicePid> : CommonDisposableNotifyPropertyChanged, ILogicalDevicePidProperty, ILogicalDevicePid, ICommonDisposable, IDisposable, INotifyPropertyChanged where TLogicalDevicePid : class, ILogicalDevicePidProperty
	{
		public const string LogTag = "LogicalDevicePidProxy";

		private TLogicalDevicePid? _devicePid;

		public TLogicalDevicePid? DevicePid
		{
			get
			{
				return _devicePid;
			}
			set
			{
				if (base.IsDisposed)
				{
					TaggedLog.Warning("LogicalDevicePidProxy", $"Pid Proxy IGNORED changed in DevicePid {value} as proxy has been disposed");
					return;
				}
				TLogicalDevicePid devicePid = _devicePid;
				if (devicePid != null)
				{
					devicePid.PropertyChanged -= DevicePidPropertyChanged;
				}
				SetBackingField(ref _devicePid, value, "DevicePid", "PidReadTimeoutSec", "PidWriteTimeoutSec", "PropertyId", "ValueRaw", "ValueState");
				if (_devicePid != null)
				{
					_devicePid!.PropertyChanged += DevicePidPropertyChanged;
				}
			}
		}

		public int PidReadTimeoutSec => DevicePid?.PidReadTimeoutSec ?? 3;

		public int PidWriteTimeoutSec => DevicePid?.PidWriteTimeoutSec ?? 6;

		public PID PropertyId => DevicePid?.PropertyId ?? PID.UNKNOWN;

		public bool IsReadOnly => DevicePid?.IsReadOnly ?? true;

		public IPidDetail PidDetail => DevicePid?.PidDetail ?? PropertyId.ConvertToPid().GetPidDetailDefault();

		public UInt48 ValueRaw
		{
			get
			{
				return DevicePid?.ValueRaw ?? default(UInt48);
			}
			set
			{
				TLogicalDevicePid? devicePid = DevicePid;
				if (devicePid == null || base.IsDisposed)
				{
					throw new PhysicalDeviceNotFoundException("LogicalDevicePidProxy", "ReadValueAsync DevicePid not setup for proxy.");
				}
				devicePid!.ValueRaw = value;
			}
		}

		public AsyncValueCachedState ValueState => DevicePid?.ValueState ?? AsyncValueCachedState.NoValue;

		public LogicalDevicePidProxy(TLogicalDevicePid? devicePid = null)
		{
			DevicePid = devicePid;
		}

		public Task<ulong> ReadValueAsync(CancellationToken cancellationToken)
		{
			TLogicalDevicePid? devicePid = DevicePid;
			if (devicePid == null || base.IsDisposed)
			{
				throw new PhysicalDeviceNotFoundException("LogicalDevicePidProxy", "ReadValueAsync DevicePid not setup for proxy.");
			}
			return devicePid!.ReadValueAsync(cancellationToken);
		}

		public async Task WriteValueAsync(ulong value, CancellationToken cancellationToken)
		{
			TLogicalDevicePid? devicePid = DevicePid;
			if (devicePid == null || base.IsDisposed)
			{
				throw new PhysicalDeviceNotFoundException("LogicalDevicePidProxy", "WriteValueAsync, DevicePid not setup for proxy.");
			}
			await devicePid!.WriteValueAsync(value, cancellationToken);
		}

		private void DevicePidPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			NotifyPropertyChanged("ValueState");
			NotifyPropertyChanged("ValueRaw");
			ValueRawPropertyChanged();
		}

		protected virtual void ValueRawPropertyChanged()
		{
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			TLogicalDevicePid devicePid = _devicePid;
			if (devicePid != null)
			{
				devicePid.PropertyChanged -= DevicePidPropertyChanged;
				_devicePid = null;
			}
		}
	}
}
