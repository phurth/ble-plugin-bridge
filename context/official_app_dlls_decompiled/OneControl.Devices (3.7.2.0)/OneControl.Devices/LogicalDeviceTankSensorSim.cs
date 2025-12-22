using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceTankSensorSim : LogicalDeviceTankSensor, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		private static readonly Random LevelRnd = new Random();

		private readonly IDisposable? _tankLevelDisposable;

		private LogicalDeviceTankSensorStatus _tankStatus = new LogicalDeviceTankSensorStatus();

		private bool _isOnline = true;

		private int _sweepDirection = 1;

		private byte _sweepLevel;

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

		private LogicalDeviceTankSensorSim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceTankSensorCapability capability, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false, int randomizeLevelOnIntervalSec = 0, bool sweepLevel = false, byte? tankCapacity = null)
			: base(logicalDeviceId, capability, deviceService, isFunctionClassChangeable)
		{
			base.AlertTankLevelThresholdsPid.DevicePid = new LogicalDevicePidSimULong(Pid.AccessorySetting1.ConvertToPid(), 255uL);
			switch (base.HoldingType)
			{
			case TankHoldingType.Supply:
				base.AlertTankLevelThresholdsPid.WriteAsync(new TankSensorAlertThreshold(TankLevelThresholdType.Low, LogicalDeviceTankSensor.LegacyTankSensorLowAlertThreshold.TankLevel), CancellationToken.None);
				break;
			case TankHoldingType.Waste:
				base.AlertTankLevelThresholdsPid.WriteAsync(new TankSensorAlertThreshold(TankLevelThresholdType.High, LogicalDeviceTankSensor.LegacyTankSensorHighAlertThreshold.TankLevel), CancellationToken.None);
				break;
			default:
				base.AlertTankLevelThresholdsPid.WriteAsync(new TankSensorAlertThreshold(TankLevelThresholdType.Unknown, LogicalDeviceTankSensor.LegacyTankSensorUnknownAlertThreshold.TankLevel), CancellationToken.None);
				break;
			}
			if (tankCapacity.HasValue)
			{
				base.TankCapacityPid.DevicePid = new LogicalDevicePidSimULong(Pid.AccessorySetting2.ConvertToPid(), 255uL);
				base.TankCapacityPid.WriteValueAsync(tankCapacity.Value, CancellationToken.None);
			}
			if (randomizeLevelOnIntervalSec > 0)
			{
				_tankLevelDisposable = ObservableExtensions.Subscribe(Observable.Interval(TimeSpan.FromSeconds(randomizeLevelOnIntervalSec)), delegate
				{
					if (ActiveConnection == LogicalDeviceActiveConnection.Direct)
					{
						byte level2 = Convert.ToByte(LevelRnd.Next(0, 100));
						_tankStatus.SetLevel(level2);
						UpdateDeviceStatus(_tankStatus.Data, 1u);
					}
				});
			}
			else
			{
				if (!sweepLevel)
				{
					return;
				}
				_tankLevelDisposable = ObservableExtensions.Subscribe(Observable.Interval(TimeSpan.FromMilliseconds(500.0)), delegate
				{
					if (ActiveConnection != LogicalDeviceActiveConnection.Direct)
					{
						return;
					}
					_sweepLevel = (byte)(_sweepLevel + 10 * _sweepDirection);
					int sweepDirection = _sweepDirection;
					int sweepDirection2;
					if (sweepDirection <= 0)
					{
						if (sweepDirection >= 0 || _sweepLevel != 0)
						{
							goto IL_004c;
						}
						sweepDirection2 = 1;
					}
					else
					{
						if (_sweepLevel != 100)
						{
							goto IL_004c;
						}
						sweepDirection2 = -1;
					}
					goto IL_0053;
					IL_0053:
					_sweepDirection = sweepDirection2;
					LogicalDeviceTankSensorStatus tankStatus = _tankStatus;
					byte level;
					if (base.IsHighPrecisionTank)
					{
						level = _sweepLevel;
					}
					else
					{
						byte b;
						switch (_sweepLevel)
						{
						case 100:
							b = 100;
							break;
						case 66:
						case 67:
						case 68:
						case 69:
						case 70:
						case 71:
						case 72:
						case 73:
						case 74:
						case 75:
						case 76:
						case 77:
						case 78:
						case 79:
						case 80:
						case 81:
						case 82:
						case 83:
						case 84:
						case 85:
						case 86:
						case 87:
						case 88:
						case 89:
						case 90:
						case 91:
						case 92:
						case 93:
						case 94:
						case 95:
						case 96:
						case 97:
						case 98:
						case 99:
							b = 66;
							break;
						case 33:
						case 34:
						case 35:
						case 36:
						case 37:
						case 38:
						case 39:
						case 40:
						case 41:
						case 42:
						case 43:
						case 44:
						case 45:
						case 46:
						case 47:
						case 48:
						case 49:
						case 50:
						case 51:
						case 52:
						case 53:
						case 54:
						case 55:
						case 56:
						case 57:
						case 58:
						case 59:
						case 60:
						case 61:
						case 62:
						case 63:
						case 64:
						case 65:
							b = 33;
							break;
						default:
							b = 0;
							break;
						}
						level = b;
					}
					tankStatus.SetLevel(level);
					UpdateDeviceStatus(_tankStatus.Data, 1u);
					return;
					IL_004c:
					sweepDirection2 = _sweepDirection;
					goto IL_0053;
				});
			}
		}

		public LogicalDeviceTankSensorSim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceTankSensorCapability capability, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false, bool sweepLevel = false)
			: this(logicalDeviceId, capability, deviceService, isFunctionClassChangeable, 0, sweepLevel)
		{
		}

		public LogicalDeviceTankSensorSim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceTankSensorCapability capability, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false, int randomizeLevelOnIntervalSec = 0, byte? tankCapcity = null)
			: this(logicalDeviceId, capability, deviceService, isFunctionClassChangeable, randomizeLevelOnIntervalSec, sweepLevel: false, tankCapcity)
		{
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_tankLevelDisposable?.TryDispose();
		}
	}
}
