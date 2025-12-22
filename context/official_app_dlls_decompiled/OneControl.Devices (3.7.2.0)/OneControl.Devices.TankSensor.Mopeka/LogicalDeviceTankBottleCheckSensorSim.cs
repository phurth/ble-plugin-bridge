using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.TankSensor.Mopeka
{
	public class LogicalDeviceTankBottleCheckSensorSim : LogicalDeviceTankSensor, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		private static readonly Random rng = new Random();

		private CancellationTokenSource _tankSensorSimTaskCancelSource = new CancellationTokenSource();

		private LogicalDeviceTankSensorStatus _tankStatus = new LogicalDeviceTankSensorStatus();

		private bool _isOnline = true;

		public override bool IsLegacyDeviceHazardous => false;

		public override LogicalDeviceActiveConnection ActiveConnection
		{
			get
			{
				if (!IsOnline)
				{
					return LogicalDeviceActiveConnection.Offline;
				}
				return LogicalDeviceActiveConnection.Direct;
			}
		}

		public virtual bool IsOnline
		{
			get
			{
				return _isOnline;
			}
			protected set
			{
				if (SetBackingField(ref _isOnline, value, "IsOnline"))
				{
					UpdateDeviceOnline(value);
				}
			}
		}

		public override ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid { get; } = new LogicalDevicePidSimFixedPoint(FixedPointType.UnsignedBigEndian16x16, PID.BATTERY_VOLTAGE, 13f);


		public override void UpdateDeviceOnline(bool online)
		{
			IsOnline = online;
		}

		public LogicalDeviceTankBottleCheckSensorSim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceTankSensorCapability(), deviceService, isFunctionClassChangeable)
		{
			Task.Run(async delegate
			{
				while (!_tankSensorSimTaskCancelSource.IsCancellationRequested)
				{
					for (int level = 30; level >= 0; level -= 2)
					{
						if (ActiveConnection == LogicalDeviceActiveConnection.Direct)
						{
							int num = rng.Next(-5, 5);
							level += num;
							LogicalDeviceTankSensorStatus logicalDeviceTankSensorStatus = new LogicalDeviceTankSensorStatus();
							logicalDeviceTankSensorStatus.SetLevel((byte)level);
							logicalDeviceTankSensorStatus.SetMeasurementQuality(100);
							logicalDeviceTankSensorStatus.SetBatteryLevel((byte)level);
							logicalDeviceTankSensorStatus.SetXAcceleration((byte)rng.Next(-127, 128));
							logicalDeviceTankSensorStatus.SetYAcceleration((byte)rng.Next(-127, 128));
							UpdateDeviceStatus(logicalDeviceTankSensorStatus);
							await TaskExtension.TryDelay(15000, _tankSensorSimTaskCancelSource.Token);
						}
					}
				}
			}, _tankSensorSimTaskCancelSource.Token);
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_tankSensorSimTaskCancelSource?.TryCancelAndDispose();
			_tankSensorSimTaskCancelSource = null;
		}
	}
}
