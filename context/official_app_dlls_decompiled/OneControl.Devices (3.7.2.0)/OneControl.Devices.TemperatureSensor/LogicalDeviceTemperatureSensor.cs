using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;

namespace OneControl.Devices.TemperatureSensor
{
	public class LogicalDeviceTemperatureSensor : LogicalDevice<LogicalDeviceTemperatureSensorStatus, LogicalDeviceTemperatureSensorCapability>, ILogicalDeviceTemperatureSensorDirect, ILogicalDeviceTemperatureSensor, ITemperatureSensor, IAccessoryDevice, ILogicalDeviceWithStatus<LogicalDeviceTemperatureSensorStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusAlertsLocap, ILogicalDeviceAccessory, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink
	{
		protected enum TemperatureSensorModeRequest
		{
			Off,
			Accessory,
			Link,
			Sleep
		}

		private const string LogTag = "LogicalDeviceTemperatureSensor";

		private const Pid PidHighTemperatureAlertThreshold = Pid.AccessorySetting1;

		private const Pid PidLowTemperatureAlertThreshold = Pid.AccessorySetting2;

		protected ILogicalDevicePidByte RequestModePid;

		public const string LowTemperatureAlertKey = "LowTemperatureAlert";

		public const string HighTemperatureAlertKey = "HighTemperatureAlert";

		public const string TemperatureInRangeAlertKey = "TemperatureInRangeAlert";

		public const string LowBatteryAlertKey = "LowBatteryAlert";

		private const int AlertsByteSize = 4;

		private const ushort LowTemperatureAlertThresholdDisabled = 32768;

		private const ushort HighTemperatureAlertThresholdDisabled = 32767;

		private UInt48 TemperatureAlertMask = ushort.MaxValue;

		private readonly ConcurrentDictionary<string, ILogicalDeviceAlert> _alertDict = new ConcurrentDictionary<string, ILogicalDeviceAlert>();

		public override bool IsLegacyDeviceHazardous => false;

		public bool AllowAutoOfflineLogicalDeviceRemoval => false;

		public float TemperatureCelsius => DeviceStatus.TemperatureCelsius;

		public bool IsTemperatureValid => DeviceStatus.IsTemperatureValid;

		public float SensorBatteryVoltage => DeviceStatus.SensorBatteryVoltage;

		public int SensorBatteryChargePercent => DeviceStatus.BatteryChargePercent;

		public bool IsSensorBatteryVoltageValid
		{
			get
			{
				if (DeviceStatus.HasData)
				{
					return base.DeviceCapability.IsCoinCellBatterySupported;
				}
				return false;
			}
		}

		public bool IsBatteryLow => DeviceStatus.IsBatteryLow;

		public bool IsTemperatureHigh => DeviceStatus.IsTemperatureHigh;

		public bool IsTemperatureLow => DeviceStatus.IsTemperatureLow;

		public bool IsTemperatureInRange => DeviceStatus.IsTemperatureInRange;

		public int LowBatteryAlertCount => DeviceStatus.LowBatteryAlertCount;

		public int TemperatureHighAlertCount => DeviceStatus.TemperatureHighAlertCount;

		public int TemperatureLowAlertCount => DeviceStatus.TemperatureLowAlertCount;

		public int TemperatureInRangeAlertCount => DeviceStatus.TemperatureInRangeAlertCount;

		public bool IsAccessoryGatewaySupported => true;

		public IEnumerable<ILogicalDeviceAlert> Alerts => _alertDict.Values;

		public LogicalDeviceTemperatureSensor(ILogicalDeviceId logicalDeviceId, LogicalDeviceTemperatureSensorCapability capability, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceTemperatureSensorStatus(), capability, deviceService, isFunctionClassChangeable)
		{
			RequestModePid = new LogicalDevicePidByte(this, PID.ACC_REQUEST_MODE, LogicalDeviceSessionType.Diagnostic);
			UpdateAlert("LowTemperatureAlert", isActive: false, null);
			UpdateAlert("HighTemperatureAlert", isActive: false, null);
			UpdateAlert("TemperatureInRangeAlert", isActive: false, null);
			UpdateAlert("LowBatteryAlert", isActive: false, null);
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			List<Action> list = new List<Action>();
			int num = 0;
			list.Insert(num, delegate
			{
				UpdateAlert("LowTemperatureAlert", DeviceStatus.IsTemperatureLow, DeviceStatus.TemperatureLowAlertCount);
			});
			num++;
			list.Insert(DeviceStatus.IsTemperatureHigh ? num : 0, delegate
			{
				UpdateAlert("HighTemperatureAlert", DeviceStatus.IsTemperatureHigh, DeviceStatus.TemperatureHighAlertCount);
			});
			num++;
			list.Insert(DeviceStatus.IsTemperatureInRange ? num : 0, delegate
			{
				UpdateAlert("TemperatureInRangeAlert", DeviceStatus.IsTemperatureInRange, DeviceStatus.TemperatureInRangeAlertCount);
			});
			num++;
			list.Insert(DeviceStatus.IsBatteryLow ? num : 0, delegate
			{
				UpdateAlert("LowBatteryAlert", DeviceStatus.IsBatteryLow, DeviceStatus.LowBatteryAlertCount);
			});
			foreach (Action item in list)
			{
				item();
			}
			NotifyPropertyChanged("TemperatureCelsius");
			NotifyPropertyChanged("IsTemperatureValid");
			NotifyPropertyChanged("SensorBatteryVoltage");
			NotifyPropertyChanged("IsSensorBatteryVoltageValid");
			NotifyPropertyChanged("SensorBatteryChargePercent");
			NotifyPropertyChanged("IsBatteryLow");
			NotifyPropertyChanged("IsTemperatureHigh");
			NotifyPropertyChanged("IsTemperatureLow");
			NotifyPropertyChanged("IsTemperatureInRange");
			NotifyPropertyChanged("LowBatteryAlertCount");
			NotifyPropertyChanged("TemperatureHighAlertCount");
			NotifyPropertyChanged("TemperatureLowAlertCount");
			NotifyPropertyChanged("TemperatureInRangeAlertCount");
		}

		public bool UpdateDeviceStatusAlerts(byte[] alertData)
		{
			try
			{
				int num = DeviceStatus.Data.Length;
				int num2 = alertData.Length;
				if (num2 != 4)
				{
					throw new ArgumentException(string.Format("{0} is expected to be {1} bytes in length.", "alertData", 4));
				}
				if (num < num2)
				{
					throw new ArgumentException(string.Format("{0} is expected to be larger than {1} bytes in length.", "Data", 4));
				}
				ArraySegment<byte> arraySegment = new ArraySegment<byte>(DeviceStatus.Data, num - num2, num2);
				if (Enumerable.SequenceEqual(arraySegment, alertData))
				{
					TaggedLog.Information("LogicalDeviceTemperatureSensor", $"{this} - Temperature Sensor UUID background advertisement found with the values: {alertData.DebugDump(0, alertData.Length)}. These values match the last scan and will be ignored.");
					return false;
				}
				for (int i = 1; i <= num2; i++)
				{
					DeviceStatus.Data[num - i] = alertData[num2 - i];
				}
				TaggedLog.Information("LogicalDeviceTemperatureSensor", $"{this} - Alert information in the Status changed from {arraySegment.DebugDump(0, num2)} to {alertData.DebugDump(0, alertData.Length)}");
				OnDeviceStatusChanged();
				return true;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceTemperatureSensor", $"{this} - Exception updating alert status {ex}: {ex.Message}");
				return false;
			}
		}

		public virtual Task<float?> TryGetTemperatureHighAlertTriggerValueAsync(TemperatureScale scale, CancellationToken cancellationToken)
		{
			return TryGetTemperatureAlertTriggerValueAsync(Pid.AccessorySetting1, 32767, scale, cancellationToken);
		}

		public virtual Task<float?> TryGetTemperatureLowAlertTriggerValueAsync(TemperatureScale scale, CancellationToken cancellationToken)
		{
			return TryGetTemperatureAlertTriggerValueAsync(Pid.AccessorySetting2, 32768, scale, cancellationToken);
		}

		private async Task<float?> TryGetTemperatureAlertTriggerValueAsync(Pid pid, ushort alertDisabledValue, TemperatureScale scale, CancellationToken cancellationToken)
		{
			UInt48 uInt = await new LogicalDevicePidUInt48(this, pid.ConvertToPid(), LogicalDeviceSessionType.Diagnostic).ReadAsync(cancellationToken);
			if ((long)uInt == alertDisabledValue)
			{
				return null;
			}
			float num = FixedPointSignedBigEndian8X8.ToFloat((short)uInt);
			return scale switch
			{
				TemperatureScale.Fahrenheit => (float)Temperature.ConvertToScale((decimal)num, TemperatureScale.Celsius, scale), 
				TemperatureScale.Celsius => num, 
				TemperatureScale.Kelvin => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
				_ => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
			};
		}

		public virtual Task TrySetTemperatureHighAlertTriggerValueAsync(float? value, TemperatureScale scale, CancellationToken cancellationToken)
		{
			return TrySetTemperatureAlertTriggerValueAsync(Pid.AccessorySetting1, 32767, value, scale, cancellationToken);
		}

		public virtual Task TrySetTemperatureLowAlertTriggerValueAsync(float? value, TemperatureScale scale, CancellationToken cancellationToken)
		{
			return TrySetTemperatureAlertTriggerValueAsync(Pid.AccessorySetting2, 32768, value, scale, cancellationToken);
		}

		private Task TrySetTemperatureAlertTriggerValueAsync(Pid pid, ushort alertDisabledValue, float? value, TemperatureScale scale, CancellationToken cancellationToken)
		{
			LogicalDevicePidUInt48 logicalDevicePidUInt = new LogicalDevicePidUInt48(this, pid.ConvertToPid(), LogicalDeviceSessionType.Diagnostic);
			UInt48 uInt = ((!value.HasValue) ? ((UInt48)alertDisabledValue) : ((UInt48)FixedPointSignedBigEndian8X8.ToFixedPoint((scale switch
			{
				TemperatureScale.Fahrenheit => (float)Temperature.ConvertToScale((decimal)value.Value, scale, TemperatureScale.Celsius), 
				TemperatureScale.Celsius => value, 
				TemperatureScale.Kelvin => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
				_ => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
			}).Value) & TemperatureAlertMask));
			UInt48? cachedPidRawValue = GetCachedPidRawValue(pid);
			if (cachedPidRawValue.HasValue && (long?)cachedPidRawValue == (long)uInt)
			{
				TaggedLog.Debug("LogicalDeviceTemperatureSensor", $"Pid Write for {pid} ignored as matches current cached value 0x{uInt:X}({value})");
				return Task.FromResult(false);
			}
			TaggedLog.Debug("LogicalDeviceTemperatureSensor", $"Pid Write for {pid} called with 0x{uInt:X}({value})");
			return logicalDevicePidUInt.WriteAsync(uInt, cancellationToken);
		}

		public void SetTemperatureAlertTriggerCachedValues(float? lowValue, float? highValue, TemperatureScale scale)
		{
			SetTemperatureAlertTriggerCachedValue(lowValue, scale, Pid.AccessorySetting2, 32768);
			SetTemperatureAlertTriggerCachedValue(highValue, scale, Pid.AccessorySetting1, 32767);
		}

		private void SetTemperatureAlertTriggerCachedValue(float? alertThreshold, TemperatureScale scale, Pid alertThresholdPid, ushort alertDisabledValue)
		{
			if (!alertThreshold.HasValue)
			{
				SetCachedPidRawValue(alertThresholdPid, alertDisabledValue);
				return;
			}
			short num = FixedPointSignedBigEndian8X8.ToFixedPoint((scale switch
			{
				TemperatureScale.Fahrenheit => (float)Temperature.ConvertToScale((decimal)alertThreshold.Value, scale, TemperatureScale.Celsius), 
				TemperatureScale.Celsius => alertThreshold, 
				TemperatureScale.Kelvin => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
				_ => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
			}).Value);
			SetCachedPidRawValue(alertThresholdPid, (UInt48)num);
		}

		public void UpdateAlert(string alertName, bool isActive, int? count)
		{
			if (_alertDict.TryGetValue(alertName, out var logicalDeviceAlert))
			{
				if (!logicalDeviceAlert.Count.HasValue && count.HasValue && ActiveConnection == LogicalDeviceActiveConnection.Offline)
				{
					_alertDict.TryAdd(alertName, new LogicalDeviceAlert(alertName, isActive, count));
					return;
				}
			}
			else if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				_alertDict.TryAdd(alertName, new LogicalDeviceAlert(alertName, isActive, count));
				return;
			}
			UpdateAlertDefaultImpl(alertName, isActive, count, _alertDict);
		}

		public void UpdateAlert(byte alertId, byte rawData)
		{
			TaggedLog.Information("LogicalDeviceTemperatureSensor", "UpdateAlert(byte, byte) is not supported for this device");
		}

		public virtual Task RequestTransitionToSleepMode(CancellationToken cancellationToken)
		{
			return RequestModePid.WriteByteAsync(3, cancellationToken);
		}

		NETWORK_STATUS ILogicalDeviceIdsCan.get_LastReceivedNetworkStatus()
		{
			return base.LastReceivedNetworkStatus;
		}
	}
}
