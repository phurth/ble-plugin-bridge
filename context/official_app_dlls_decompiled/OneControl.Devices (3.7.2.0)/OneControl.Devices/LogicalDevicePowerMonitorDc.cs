using System;
using System.ComponentModel;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using Serilog;

namespace OneControl.Devices
{
	public class LogicalDevicePowerMonitorDc : LogicalDevice<LogicalDevicePowerMonitorDcStatus, ILogicalDevicePowerMonitorDcCapability>, ILogicalDevicePowerMonitorDc, ILogicalDeviceWithStatus<LogicalDevicePowerMonitorDcStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		public const ushort BatteryCapacityAmpHoursInvalidValue = ushort.MaxValue;

		public ILogicalDevicePidFixedPoint BatteryCapacityAmpHoursPid { get; protected set; }

		public LogicalDevicePowerMonitorDc(ILogicalDeviceId logicalDeviceId, ILogicalDevicePowerMonitorDcCapability capability, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDevicePowerMonitorDcStatus(), capability, service, isFunctionClassChangeable)
		{
			BatteryCapacityAmpHoursPid = new LogicalDevicePidFixedPoint(FixedPointType.UnsignedBigEndian16x16, this, PID.BATTERY_CAPACITY_AMP_HOURS, LogicalDeviceSessionType.None, (ulong checkValue) => (ushort)checkValue != ushort.MaxValue);
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			Log.Information("Data Changed {LogicalId}: Status = {DeviceStatus}", base.LogicalId, DeviceStatus);
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}
}
