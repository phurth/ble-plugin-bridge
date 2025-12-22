using System;
using System.ComponentModel;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using Serilog;

namespace OneControl.Devices
{
	public class LogicalDevicePowerMonitorAc : LogicalDevice<LogicalDevicePowerMonitorAcStatus, ILogicalDeviceCapability>, ILogicalDevicePowerMonitorAc, ILogicalDeviceWithStatus<LogicalDevicePowerMonitorAcStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		public const ushort ShorePowerAmpRatingInvalidValue = ushort.MaxValue;

		public ILogicalDevicePidFixedPoint ShorePowerAmpRatingPid { get; protected set; }

		public LogicalDevicePowerMonitorAc(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDevicePowerMonitorAcStatus(), (ILogicalDeviceCapability)new LogicalDeviceCapability(), service, isFunctionClassChangeable)
		{
			ShorePowerAmpRatingPid = new LogicalDevicePidFixedPoint(FixedPointType.UnsignedBigEndian16x16, this, PID.SHORE_POWER_AMP_RATING, LogicalDeviceSessionType.None, (ulong checkValue) => (ushort)checkValue != ushort.MaxValue);
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
