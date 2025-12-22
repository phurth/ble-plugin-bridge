using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using Serilog;

namespace OneControl.Devices
{
	public class LogicalDeviceChassisInfo : LogicalDevice<LogicalDeviceChassisInfoStatus, ILogicalDeviceCapability>, IChassisInfo, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceMyRvLink
	{
		private ILogicalDevicePidFixedPoint _batteryVoltagePidCan;

		public ParkBrake ParkBreak => DeviceStatus.ParkBreak;

		public IgnitionPowerSignal IgnitionPowerSignal => DeviceStatus.IgnitionPowerSignal;

		public virtual LogicalDeviceExScope VoltageMeasurementBatteryPidScope => LogicalDeviceExScope.Product;

		public virtual ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid => _batteryVoltagePidCan ?? (_batteryVoltagePidCan = new LogicalDevicePidFixedPoint(FixedPointType.UnsignedBigEndian16x16, this, PID.BATTERY_VOLTAGE, LogicalDeviceSessionType.None));

		public bool IsVoltagePidReadSupported => true;

		public LogicalDeviceChassisInfo(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceChassisInfoStatus(), (ILogicalDeviceCapability)new LogicalDeviceCapability(), service, isFunctionClassChangeable)
		{
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			Log.Information("Data Changed {LogicalId}: ParkBreak = {ParkBreak}, IgnitionPowerSignal = {IgnitionPowerSignal}", base.LogicalId, DeviceStatus.ParkBreak, DeviceStatus.IgnitionPowerSignal);
			OnPropertyChanged("ParkBreak");
			OnPropertyChanged("IgnitionPowerSignal");
		}

		public Task<float> ReadVoltageMeasurementAsync(CancellationToken cancellationToken)
		{
			return LogicalDeviceVoltageExtension.ReadVoltageMeasurementAsync(VoltageMeasurementBatteryPid, cancellationToken);
		}
	}
}
