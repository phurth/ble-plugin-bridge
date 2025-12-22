using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.TPMS
{
	public class LogicalDeviceTpmsStatus : LogicalDeviceStatusPacketMutable, ILogicalDeviceStatus<LogicalDeviceTpmsStatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		private const string LogTag = "LogicalDeviceTpmsStatus";

		public const int MinimumStatusPacketSize = 5;

		private const int SensorCountIndex = 0;

		private const int VehicleConfigSequenceIndex = 1;

		private const int SensorConfigSequenceIndex = 2;

		private const int RepeaterModeIndex = 3;

		private const int RepeaterBatteryHwStatusIndex = 4;

		private const byte RepeaterBatteryLowBitmask = 8;

		private const byte RepeaterPowerSourceBitmask = 16;

		private const byte RepeaterHwFaultBitmask = 32;

		private const byte RepeaterChargingStatusBitmask = 64;

		private const byte HasBatteryBitmask = 128;

		private static readonly BitPositionValue RepeaterVoltageLevelBitPosition = new BitPositionValue(7u, 4);

		public byte LearnedSensorCount => base.Data[0];

		public byte VehicleConfigSequence => base.Data[1];

		public byte SensorConfigSequence => base.Data[2];

		public TpmsRepeaterMode TpmsRepeaterMode
		{
			get
			{
				byte b = base.Data[3];
				if (!Enum.IsDefined(typeof(TpmsRepeaterMode), (int)b))
				{
					TaggedLog.Warning("LogicalDeviceTpmsStatus", $"Current TPMS Repeater Mode is invalid, raw value: {b}");
					return TpmsRepeaterMode.Unknown;
				}
				return (TpmsRepeaterMode)b;
			}
		}

		public TpmsRepeaterVoltageLevel TpmsRepeaterVoltageLevel
		{
			get
			{
				uint num = RepeaterVoltageLevelBitPosition.DecodeValueFromBuffer(base.Data);
				if (!Enum.IsDefined(typeof(TpmsRepeaterVoltageLevel), (int)num))
				{
					TaggedLog.Warning("LogicalDeviceTpmsStatus", $"Current TPMS Repeater Voltage Level is invalid, raw value: {num}");
					return TpmsRepeaterVoltageLevel.Error;
				}
				return (TpmsRepeaterVoltageLevel)num;
			}
		}

		public bool RepeaterBatteryLow => GetBit(BasicBitMask.BitMask0X08, 4);

		public TpmsRepeaterPowerSource RepeaterPowerSource
		{
			get
			{
				if (!GetBit(BasicBitMask.BitMask0X10, 4))
				{
					return TpmsRepeaterPowerSource.TwelveVoltSource;
				}
				return TpmsRepeaterPowerSource.LiPoBatterySource;
			}
		}

		public bool RepeaterHasHardwareFault => GetBit(BasicBitMask.BitMask0X20, 4);

		public bool RepeaterCharging => GetBit(BasicBitMask.BitMask0X40, 4);

		public bool RepeaterHasBattery => !GetBit(BasicBitMask.BitMask0X80, 4);

		public void SetSensorCount(byte sensorCount)
		{
			SetByte(byte.MaxValue, sensorCount, 0);
		}

		public void SetVehicleConfigSequence(byte axleConfigSequence)
		{
			SetByte(byte.MaxValue, axleConfigSequence, 1);
		}

		public void SetSensorConfigSequence(byte sensorConfigSequence)
		{
			SetByte(byte.MaxValue, sensorConfigSequence, 2);
		}

		public void SetRepeaterMode(TpmsRepeaterMode tpmsRepeaterMode)
		{
			SetByte(byte.MaxValue, (byte)tpmsRepeaterMode, 3);
		}

		public void SetRepeaterVoltageLevel(TpmsRepeaterVoltageLevel voltageLevel)
		{
			RepeaterVoltageLevelBitPosition.EncodeValueToBuffer((uint)voltageLevel, base.Data);
		}

		public void SetRepeaterBatteryLow(bool batteryLow)
		{
			SetBit(BasicBitMask.BitMask0X08, batteryLow);
		}

		public void SetRepeaterPowerSource(TpmsRepeaterPowerSource powerSource)
		{
			SetBit(BasicBitMask.BitMask0X10, powerSource == TpmsRepeaterPowerSource.LiPoBatterySource);
		}

		public void SetRepeaterHasHardwareFault(bool hasHardwareFault)
		{
			SetBit(BasicBitMask.BitMask0X20, hasHardwareFault);
		}

		public void SetRepeaterCharging(bool charging)
		{
			SetBit(BasicBitMask.BitMask0X40, charging);
		}

		public void SetRepeaterHasBattery(bool hasBattery)
		{
			SetBit(BasicBitMask.BitMask0X80, hasBattery);
		}

		public LogicalDeviceTpmsStatus()
			: base(5u)
		{
		}

		public LogicalDeviceTpmsStatus(LogicalDeviceTpmsStatus originalStatus)
		{
			byte[] data = originalStatus.Data;
			Update(data, data.Length);
		}

		public override string ToString()
		{
			return $"Device Status: {base.ToString()} LearnedSensorCount: {LearnedSensorCount} VehicleConfigSequence: {VehicleConfigSequence} " + $"SensorConfigSequence: {SensorConfigSequence} TpmsRepeaterMode: {TpmsRepeaterMode} TpmsRepeaterVoltageLevel: {TpmsRepeaterVoltageLevel} " + $"RepeaterBatteryLow: {RepeaterBatteryLow} RepeaterPowerSource: {RepeaterPowerSource} RepeaterHasHardwareFault: {RepeaterHasHardwareFault} " + $"RepeaterCharging: {RepeaterCharging} RepeaterHasBattery: {RepeaterHasBattery}";
		}

		public LogicalDeviceTpmsStatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceTpmsStatusSerializable(this);
		}
	}
}
