using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Remote;
using Serilog;

namespace OneControl.Devices
{
	public class LogicalDeviceHourMeter : LogicalDevice<LogicalDeviceHourMeterStatus, ILogicalDeviceCapability>, ILogicalDeviceHourMeterDirect, ILogicalDeviceHourMeter, IHourMeter, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatus<LogicalDeviceHourMeterStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceWithStatusUpdate<LogicalDeviceHourMeterStatus>, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink, ILogicalDeviceHourMeterRemote, ILogicalDeviceRemote
	{
		public ILogicalDevicePidTimeSpan HourMeterLastMaintenanceTimePid { get; protected set; }

		public ILogicalDevicePidTimeSpan HourMeterMaintenancePeriodSecPid { get; protected set; }

		public RemoteOnline RemoteOnline { get; protected set; }

		public override bool IsLegacyDeviceHazardous => false;

		public bool Error => DeviceStatus.Error;

		public bool MaintenancePastDue => DeviceStatus.MaintenancePastDue;

		public bool MaintenanceDue => DeviceStatus.MaintenanceDue;

		public bool Running => DeviceStatus.Running;

		public ulong OperatingSeconds => DeviceStatus.OperatingSeconds;

		public override bool IsRemoteAccessAvailable => DeviceService.RemoteManager?.IsRemoteAccessAvailable(this, RemoteOnline?.Channel) ?? false;

		public IRemoteChannelDefOnline RemoteOnlineChannel => RemoteOnline?.Channel;

		public LogicalDeviceHourMeter(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceHourMeterStatus(), (ILogicalDeviceCapability)new LogicalDeviceCapability(), service, isFunctionClassChangeable)
		{
			RemoteOnline = new RemoteOnline(this, RemoteChannels);
			HourMeterLastMaintenanceTimePid = new LogicalDevicePidTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt32Seconds, this, PID.LAST_MAINTENANCE_TIME_SEC, LogicalDeviceSessionType.Diagnostic);
			HourMeterMaintenancePeriodSecPid = new LogicalDevicePidTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt32Seconds, this, PID.MAINTENANCE_PERIOD_SEC, LogicalDeviceSessionType.Diagnostic);
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			Log.Information("Data Changed {LogicalId}: Running = {Running}, Stopping = {Stopping}, Error = {Error}, OperatingSec = {OperatingSeconds}", base.LogicalId, DeviceStatus.Running, DeviceStatus.Stopping, DeviceStatus.Error, DeviceStatus.OperatingSeconds);
			OnPropertyChanged("Error");
			OnPropertyChanged("MaintenancePastDue");
			OnPropertyChanged("MaintenanceDue");
			OnPropertyChanged("Running");
			OnPropertyChanged("OperatingSeconds");
		}

		public static IHourMeter FindAssociatedHourMeter(ILogicalDevice parentLogicalDevice)
		{
			ILogicalDeviceService deviceService = parentLogicalDevice.DeviceService;
			if (deviceService == null)
			{
				return null;
			}
			PRODUCT_ID productId = parentLogicalDevice.LogicalId.ProductId;
			MAC macAddress = parentLogicalDevice.LogicalId.ProductMacAddress;
			List<ILogicalDeviceHourMeter> list = deviceService.DeviceManager?.FindLogicalDevices((ILogicalDeviceHourMeter foundLogicalDevice) => foundLogicalDevice.LogicalId.IsMatchingPhysicalHardware(productId, (byte)12, 0, macAddress));
			if (list == null || list.Count == 0)
			{
				Log.Debug("{ParentLogicalDevice} found no HourMeter", parentLogicalDevice);
				return null;
			}
			if (list.Count == 1)
			{
				return Enumerable.First(list);
			}
			Log.Debug("{ParentLogicalDevice} found multiple potential HourMeters", parentLogicalDevice);
			IHourMeter hourMeter = null;
			foreach (ILogicalDeviceHourMeter item in list)
			{
				if (item.ActiveConnection != 0)
				{
					return item;
				}
				if (hourMeter == null)
				{
					hourMeter = item;
				}
			}
			return hourMeter;
		}

		public override void Dispose(bool disposing)
		{
			RemoteOnline?.TryDispose();
			RemoteOnline = null;
		}

		NETWORK_STATUS ILogicalDeviceIdsCan.get_LastReceivedNetworkStatus()
		{
			return base.LastReceivedNetworkStatus;
		}

		TRemoteChannelDef ILogicalDeviceRemote.GetRemoteChannelForChannelId<TRemoteChannelDef>(string channelId)
		{
			return GetRemoteChannelForChannelId<TRemoteChannelDef>(channelId);
		}
	}
}
