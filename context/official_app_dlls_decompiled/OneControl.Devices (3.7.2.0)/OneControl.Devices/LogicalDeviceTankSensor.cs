using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;
using OneControl.Devices.Remote;
using OneControl.Devices.TankSensor;

namespace OneControl.Devices
{
	public class LogicalDeviceTankSensor : LogicalDevice<LogicalDeviceTankSensorStatus, ILogicalDeviceTankSensorCapability>, ILogicalDeviceDirectTankSensor, ILogicalDeviceTankSensor, ITankSensor, ILogicalDeviceWithStatus<LogicalDeviceTankSensorStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceTankSensorStatus>, ILogicalDeviceWithStatusAlerts, ILogicalDeviceWithStatusAlertsLocap, ILogicalDeviceAccessory, IAccessoryDevice, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink, ILogicalDeviceRemoteTankSensor, ILogicalDeviceRemote
	{
		public class TankOrientationData
		{
			public bool IsHorizontal { get; }

			public ushort HeightOrDiameterMm { get; }

			public ushort? MinHeightMm { get; }

			public TankOrientationData(bool isHorizontal, ushort heightOrDiameterMm, ushort? minHeightMm)
			{
				IsHorizontal = isHorizontal;
				HeightOrDiameterMm = heightOrDiameterMm;
				MinHeightMm = minHeightMm;
			}
		}

		public const string LogTag = "LogicalDeviceTankSensor";

		public const string TankLevelAlertKey = "TankLevelAlert";

		public const string TankLowThresholdExceededAlertKey = "TankLowThresholdExceededAlert";

		public const string TankHighThresholdExceededAlertKey = "TankHighThresholdExceededAlert";

		public const string TankUnknownThresholdExceededAlertKey = "TankUnknownThresholdExceededAlert";

		public const string LowBatteryAlertKey = "LowBatteryAlert";

		private const byte UnknownLowThreshold = 0;

		private const byte UnknownHighThreshold = byte.MaxValue;

		public static readonly TankSensorAlertThreshold LegacyTankSensorHighAlertThreshold = new TankSensorAlertThreshold(TankLevelThresholdType.High, 66);

		public static readonly TankSensorAlertThreshold LegacyTankSensorLowAlertThreshold = new TankSensorAlertThreshold(TankLevelThresholdType.Low, 33);

		public static readonly TankSensorAlertThreshold LegacyTankSensorUnknownAlertThreshold = new TankSensorAlertThreshold(TankLevelThresholdType.Unknown, 0);

		private LogicalDevicePidULong _tankHeightOrientationPid;

		private const int BackgroundAlertDelayMs = 30000;

		private readonly ConcurrentDictionary<string, ILogicalDeviceAlert> _alertDict = new ConcurrentDictionary<string, ILogicalDeviceAlert>();

		private const byte AlertActiveFlag = 128;

		private const byte AlertCountBitmask = 127;

		public const int MaxAlertCount = 2;

		private ILogicalDevicePidFixedPoint _batteryVoltagePidCan;

		private static PRODUCT_ID MopekaLpSensorProductId = PRODUCT_ID.BOTTLECHECK_WIRELESS_LP_TANK_SENSOR;

		private static PRODUCT_ID LoCapLpSensorProductId = PRODUCT_ID.LP_TANK_SENSOR_ASSEMBLY;

		public RemoteOnline RemoteOnline { get; protected set; }

		public RemoteTankSensor RemoteTankSensor { get; protected set; }

		public override bool IsLegacyDeviceHazardous => false;

		public byte Level => DeviceStatus.Level;

		public byte? BatteryLevel => DeviceStatus.BatteryLevel;

		public byte? MeasurementQuality => DeviceStatus.MeasurementQuality;

		public float? XAcceleration => DeviceStatus.XAcceleration;

		public float? YAcceleration => DeviceStatus.YAcceleration;

		public TankSensorType SensorType => GetTankSensorType(base.LogicalId);

		public TankHoldingType HoldingType => SensorType.GetAttribute<TankSensorTypeAttribute>()?.HoldingType ?? TankHoldingType.Unknown;

		public bool AllowAutoOfflineLogicalDeviceRemoval => IsEmbeddedTank;

		public override bool IsRemoteAccessAvailable => DeviceService.RemoteManager?.IsRemoteAccessAvailable(this, RemoteTankSensor?.Channel) ?? false;

		public IRemoteChannelDefOnline RemoteOnlineChannel => RemoteOnline?.Channel;

		public IEnumerable<ILogicalDeviceAlert> Alerts => _alertDict.Values;

		public virtual LogicalDeviceExScope VoltageMeasurementBatteryPidScope => LogicalDeviceExScope.Product;

		public virtual ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid => _batteryVoltagePidCan ?? (_batteryVoltagePidCan = new LogicalDevicePidFixedPoint(FixedPointType.UnsignedBigEndian16x16, this, PID.BATTERY_VOLTAGE, LogicalDeviceSessionType.None));

		public bool IsVoltagePidReadSupported => IsEmbeddedTank;

		public bool IsEmbeddedTank
		{
			get
			{
				if (base.LogicalId.ProductId != MopekaLpSensorProductId)
				{
					return base.LogicalId.ProductId != LoCapLpSensorProductId;
				}
				return false;
			}
		}

		public LogicalDevicePidProxyTankSensorCapacity TankCapacityPid { get; }

		public bool IsTankCapacitySupported => base.DeviceCapability.IsTankCapacitySupported;

		public TankSensorCapacity? TankCapacity
		{
			get
			{
				if (!IsTankCapacitySupported)
				{
					return null;
				}
				return TankCapacityPid.Capacity;
			}
			set
			{
				TankCapacityPid.Capacity = (value.HasValue ? value.Value : TankCapacityPid.Capacity);
			}
		}

		public AsyncValueCachedState TankCapacityState => TankCapacityPid.ValueState;

		public LogicalDevicePidProxyTankSensorAlertThreshold AlertTankLevelThresholdsPid { get; }

		public bool IsTankLevelOutsideThreshold
		{
			get
			{
				if (IsSetAlertTankLevelThresholdSupported)
				{
					return DeviceStatus.IsTankLevelAlertActive;
				}
				if (SensorType == TankSensorType.UnknownTank)
				{
					return false;
				}
				TankSensorAlertThreshold legacyAlertTankLevelThreshold = GetLegacyAlertTankLevelThreshold();
				bool num = legacyAlertTankLevelThreshold.ThresholdType == TankLevelThresholdType.High && Level >= legacyAlertTankLevelThreshold.TankLevel;
				bool flag = legacyAlertTankLevelThreshold.ThresholdType == TankLevelThresholdType.Low && Level <= legacyAlertTankLevelThreshold.TankLevel;
				return num || flag;
			}
		}

		[Obsolete("Use the SensorPrecisionType property instead")]
		public bool IsHighPrecisionTank => base.DeviceCapability.SensorPrecisionType == SensorPrecisionType.HighPrecision;

		public SensorPrecisionType SensorPrecisionType => base.DeviceCapability.SensorPrecisionType;

		public bool IsLegacyTank
		{
			get
			{
				if (!IsHighPrecisionTank && !IsSetAlertTankLevelThresholdSupported)
				{
					return IsEmbeddedTank;
				}
				return false;
			}
		}

		public bool IsSetAlertTankLevelThresholdSupported => base.DeviceCapability.AreTankAlertsSupported;

		public TankSensorAlertThreshold AlertThreshold
		{
			get
			{
				if (!IsLegacyTank)
				{
					return AlertTankLevelThresholdsPid.ValueAlertThreshold;
				}
				return GetLegacyAlertTankLevelThreshold();
			}
			set
			{
				AlertTankLevelThresholdsPid.ValueAlertThreshold = value;
			}
		}

		public AsyncValueCachedState AlertThresholdState => AlertTankLevelThresholdsPid.ValueState;

		public bool IsTankHeightOrientationSupported => base.DeviceCapability.IsTankHeightOrientationSupported;

		public bool IsAccessoryGatewaySupported
		{
			get
			{
				if (base.LogicalId.ProductId != MopekaLpSensorProductId)
				{
					return base.LogicalId.ProductId == LoCapLpSensorProductId;
				}
				return true;
			}
		}

		public LogicalDeviceTankSensor(ILogicalDeviceId logicalDeviceId, ILogicalDeviceTankSensorCapability capability, ILogicalDeviceService deviceService, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceTankSensorStatus(), capability, deviceService, isFunctionClassChangeable)
		{
			AlertTankLevelThresholdsPid = new LogicalDevicePidProxyTankSensorAlertThreshold(this, Pid.AccessorySetting1, LogicalDeviceSessionType.Diagnostic);
			AlertTankLevelThresholdsPid.PropertyChanged += AlertTankLevelThresholdsPidOnPropertyChanged;
			TankCapacityPid = new LogicalDevicePidProxyTankSensorCapacity(this, Pid.AccessorySetting2, LogicalDeviceSessionType.Diagnostic);
			TankCapacityPid.PropertyChanged += TankCapacityPidOnPropertyChanged;
			_tankHeightOrientationPid = new LogicalDevicePidULong(this, Pid.AccessorySetting3.ConvertToPid(), LogicalDeviceSessionType.Diagnostic);
			RemoteOnline = new RemoteOnline(this, RemoteChannels);
			RemoteTankSensor = new RemoteTankSensor(this, RemoteChannels);
			UpdateAlert("TankLevelAlert", isActive: false, null);
			UpdateAlert("LowBatteryAlert", isActive: false, null);
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			NotifyPropertyChanged("Level");
			NotifyPropertyChanged("BatteryLevel");
			NotifyPropertyChanged("MeasurementQuality");
			NotifyPropertyChanged("XAcceleration");
			NotifyPropertyChanged("YAcceleration");
			NotifyPropertyChanged("IsTankLevelOutsideThreshold");
			UpdateAlert("TankLevelAlert", DeviceStatus.IsTankLevelAlertActive, DeviceStatus.TankLevelAlertCount);
		}

		public override void OnLogicalIdChanged()
		{
			base.OnLogicalIdChanged();
			NotifyPropertyChanged("SensorType");
			NotifyPropertyChanged("HoldingType");
		}

		private static TankSensorType GetTankSensorType(ILogicalDeviceId logicalDeviceId)
		{
			if (DeviceCategory.GetDeviceCategory(logicalDeviceId) == DeviceCategory.LiquidPropane)
			{
				return TankSensorType.LpTank;
			}
			if (DeviceCategory.GetDeviceCategory(logicalDeviceId) != DeviceCategory.Tank)
			{
				return TankSensorType.UnknownTank;
			}
			string text = logicalDeviceId.FunctionName.Name.ToUpper();
			if (text.Contains(TankSensorType.BlackTank.GetAttribute<TankSensorTypeAttribute>()!.FunctionNameIdentifier))
			{
				return TankSensorType.BlackTank;
			}
			if (text.Contains(TankSensorType.GreyTank.GetAttribute<TankSensorTypeAttribute>()!.FunctionNameIdentifier))
			{
				return TankSensorType.GreyTank;
			}
			if (text.Contains(TankSensorType.FreshTank.GetAttribute<TankSensorTypeAttribute>()!.FunctionNameIdentifier))
			{
				return TankSensorType.FreshTank;
			}
			if (text.Contains(TankSensorType.FuelTank.GetAttribute<TankSensorTypeAttribute>()!.FunctionNameIdentifier))
			{
				return TankSensorType.FuelTank;
			}
			if (text.Contains(TankSensorType.LpTank.GetAttribute<TankSensorTypeAttribute>()!.FunctionNameIdentifier))
			{
				return TankSensorType.LpTank;
			}
			return TankSensorType.UnknownTank;
		}

		public void UpdateAlert(string alertName, bool isActive, int? count)
		{
			if (alertName == "LowBatteryAlert")
			{
				UpdateAlertDefaultImpl(alertName, isActive, count, _alertDict);
				return;
			}
			switch (SensorType.GetAttribute<TankSensorTypeAttribute>()!.ThresholdType)
			{
			case TankLevelThresholdType.Low:
				UpdateAlertDefaultImpl("TankLowThresholdExceededAlert", isActive, count, _alertDict);
				break;
			case TankLevelThresholdType.High:
				UpdateAlertDefaultImpl("TankHighThresholdExceededAlert", isActive, count, _alertDict);
				break;
			default:
				UpdateAlertDefaultImpl("TankUnknownThresholdExceededAlert", isActive, count, _alertDict);
				break;
			}
		}

		public void UpdateAlert(byte alertId, byte rawData)
		{
			bool flag = (rawData & 0x80) != 0;
			byte b = (byte)(rawData & 0x7Fu);
			switch (alertId)
			{
			case 0:
				if (!(base.LastUpdatedTimestamp > DateTime.Now.AddMilliseconds(-30000.0)))
				{
					UpdateAlert("TankLevelAlert", flag, b);
					if (DeviceStatus.IsTankLevelAlertActive != flag || DeviceStatus.TankLevelAlertCount != b)
					{
						DeviceStatus.SetTankLevelAlert(flag, b);
						OnDeviceStatusChanged();
					}
				}
				break;
			case 1:
				UpdateAlert("LowBatteryAlert", flag, b);
				break;
			default:
				TaggedLog.Warning("LogicalDeviceTankSensor", $"Received and ignoring unknown Alert Id 0x{alertId:X} for {this}");
				break;
			}
		}

		public bool UpdateDeviceStatusAlerts(byte[] alertData)
		{
			try
			{
				int num = alertData.Length;
				if (num == 0)
				{
					return false;
				}
				byte b = 0;
				while (b < 2)
				{
					TankSensorAlertId tankSensorAlertId = (TankSensorAlertId)b;
					if ((uint)tankSensorAlertId <= 1u)
					{
						UpdateAlert(b, alertData[b]);
						b = (byte)(b + 1);
						continue;
					}
					throw new ArgumentException($"Invalid Status Alert Size of {num}: {alertData.DebugDump()} for {this}");
				}
				return true;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceTankSensor", $"{this} - Exception updating alert status {ex}: {ex.Message}");
				return false;
			}
		}

		public Task<float> ReadVoltageMeasurementAsync(CancellationToken cancellationToken)
		{
			return LogicalDeviceVoltageExtension.ReadVoltageMeasurementAsync(VoltageMeasurementBatteryPid, cancellationToken);
		}

		public async Task<TankSensorCapacity?> GetTankCapacityAsync(CancellationToken cancellationToken)
		{
			if (IsTankCapacitySupported)
			{
				return await TankCapacityPid.ReadAsync(cancellationToken);
			}
			return null;
		}

		public Task SetTankCapacity(TankSensorCapacity value, CancellationToken cancellationToken)
		{
			return TankCapacityPid.WriteAsync(value, cancellationToken);
		}

		private void TankCapacityPidOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			NotifyPropertyChanged("TankCapacity");
		}

		public async Task<TankSensorAlertThreshold> GetAlertTankLevelThresholdAsync(CancellationToken cancellationToken)
		{
			if (HoldingType == TankHoldingType.Unknown)
			{
				throw new TankSensorHoldingTypeUnknownException(this);
			}
			TankSensorAlertThreshold tankSensorAlertThreshold;
			TankSensorAlertThreshold result;
			if (IsSetAlertTankLevelThresholdSupported)
			{
				tankSensorAlertThreshold = await AlertTankLevelThresholdsPid.ReadAsync(cancellationToken);
				TankLevelThresholdType thresholdType = tankSensorAlertThreshold.ThresholdType;
				TankHoldingType holdingType = HoldingType;
				if (thresholdType != TankLevelThresholdType.Unknown)
				{
					goto IL_00d2;
				}
				if (holdingType != 0)
				{
					if (holdingType != TankHoldingType.Waste)
					{
						goto IL_00d2;
					}
					result = new TankSensorAlertThreshold(TankLevelThresholdType.High, byte.MaxValue);
				}
				else
				{
					result = new TankSensorAlertThreshold(TankLevelThresholdType.Low, 0);
				}
				goto IL_00d5;
			}
			return GetLegacyAlertTankLevelThreshold();
			IL_00d2:
			result = tankSensorAlertThreshold;
			goto IL_00d5;
			IL_00d5:
			return result;
		}

		private TankSensorAlertThreshold GetLegacyAlertTankLevelThreshold()
		{
			return HoldingType switch
			{
				TankHoldingType.Supply => LegacyTankSensorLowAlertThreshold, 
				TankHoldingType.Waste => LegacyTankSensorHighAlertThreshold, 
				TankHoldingType.Unknown => LegacyTankSensorUnknownAlertThreshold, 
				_ => LegacyTankSensorUnknownAlertThreshold, 
			};
		}

		public Task SetAlertTankLevelThreshold(TankSensorAlertThreshold alertThreshold, CancellationToken cancellationToken)
		{
			return AlertTankLevelThresholdsPid.WriteAsync(alertThreshold, cancellationToken);
		}

		private void AlertTankLevelThresholdsPidOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			NotifyPropertyChanged("AlertThreshold");
			NotifyPropertyChanged("AlertThresholdState");
		}

		public Task SetVerticalTankConfigurationAsync(int heightInMillimeters, CancellationToken cancellationToken = default(CancellationToken))
		{
			return SetVerticalTankConfigurationAsync(38, (ushort)heightInMillimeters, cancellationToken);
		}

		public Task SetHorizontalTankConfigurationAsync(int heightInMillimeters, CancellationToken cancellationToken = default(CancellationToken))
		{
			return SetHorizontalTankConfigurationAsync((ushort)heightInMillimeters, cancellationToken);
		}

		public async Task SetVerticalTankConfigurationAsync(ushort minHeightMm, ushort heightMm, CancellationToken cancellationToken)
		{
			if (!IsTankHeightOrientationSupported)
			{
				throw new NotSupportedException("Tank configuration not supported by this device.");
			}
			ulong num = 0uL;
			num |= (ulong)(int)(minHeightMm & 0x1FFu);
			num |= (ulong)((long)(heightMm & 0x3FFF) << 9);
			num |= 0;
			TaggedLog.Debug("LogicalDeviceTankSensor", $"Writing tank config: minHeight={minHeightMm}, height={heightMm}");
			await _tankHeightOrientationPid.WriteAsync(num, cancellationToken);
		}

		public async Task SetHorizontalTankConfigurationAsync(ushort diameterMm, CancellationToken cancellationToken)
		{
			if (!IsTankHeightOrientationSupported)
			{
				throw new NotSupportedException("Tank configuration not supported by this device.");
			}
			ulong num = 0uL;
			num |= (ulong)((long)(diameterMm & 0x3FFF) << 9);
			num |= 0x800000;
			await _tankHeightOrientationPid.WriteAsync(num, cancellationToken);
		}

		public async Task<TankOrientationData?> GetTankOrientationDataAsync(CancellationToken cancellationToken)
		{
			if (!IsTankHeightOrientationSupported)
			{
				return null;
			}
			ulong num = await _tankHeightOrientationPid.ReadAsync(cancellationToken);
			ushort value = (ushort)(num & 0x1FF);
			ushort heightOrDiameterMm = (ushort)((num >> 9) & 0x3FFF);
			bool flag = ((num >> 23) & 1) == 1;
			return new TankOrientationData(flag, heightOrDiameterMm, flag ? null : new ushort?(value));
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			AlertTankLevelThresholdsPid.PropertyChanged -= AlertTankLevelThresholdsPidOnPropertyChanged;
			AlertTankLevelThresholdsPid.TryDispose();
			TankCapacityPid.PropertyChanged -= TankCapacityPidOnPropertyChanged;
			TankCapacityPid.TryDispose();
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
