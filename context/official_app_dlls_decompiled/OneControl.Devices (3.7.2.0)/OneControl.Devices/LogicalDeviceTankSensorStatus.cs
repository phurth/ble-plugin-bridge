using System.ComponentModel;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.TankSensor;

namespace OneControl.Devices
{
	public class LogicalDeviceTankSensorStatus : LogicalDeviceStatusPacketMutable, ILogicalDeviceStatus<LogicalDeviceTankSensorStatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		public static readonly int MaximumStatusPacketSize = 8;

		public static readonly int PercentByteIndex = 0;

		public static readonly int BatteryLevelIndex = 1;

		public static readonly int MeasurementQualityIndex = 2;

		public static readonly int XAccelerationIndex = 3;

		public static readonly int YAccelerationIndex = 4;

		public static readonly int TankLevelAlertIndex = 5;

		public static readonly int UserMessageStartIndex = 6;

		public const byte BatteryLevelUnknown = byte.MaxValue;

		public const byte MeasurementQualityUnknown = byte.MaxValue;

		public const byte StatusBitmaskPercent = 127;

		public const byte MeasurementQualityBitmask = 127;

		public const byte FullByteBitMask = byte.MaxValue;

		public const byte AlertActiveBitmask = 128;

		public const byte AlertCountBitmask = 127;

		public const byte AccelUnknown = 128;

		public const float AccelCoefficient = 0.0009765625f;

		public const int IdsCanTankSensorV1StatusSize = 1;

		public const int MopekaProprietaryProtocolSize = 5;

		public const int IdsCanTankSensorHighPrecisionStatusSize = 7;

		public byte Level => (byte)MathCommon.Clamp(base.Data[PercentByteIndex] & 0x7F, 0, 100);

		public byte? BatteryLevel
		{
			get
			{
				if (base.Data.Length < MaximumStatusPacketSize || base.Data[BatteryLevelIndex] > 100)
				{
					return null;
				}
				return base.Data[BatteryLevelIndex];
			}
		}

		public byte? MeasurementQuality
		{
			get
			{
				if (base.Data.Length < MaximumStatusPacketSize || base.Data[MeasurementQualityIndex] > 100)
				{
					return null;
				}
				return base.Data[MeasurementQualityIndex];
			}
		}

		public float? XAcceleration
		{
			get
			{
				if (base.Data.Length >= MaximumStatusPacketSize && base.Data[XAccelerationIndex] != 128)
				{
					return (float)(sbyte)base.Data[XAccelerationIndex] * 0.0009765625f;
				}
				return null;
			}
		}

		public float? YAcceleration
		{
			get
			{
				if (base.Data.Length >= MaximumStatusPacketSize && base.Data[YAccelerationIndex] != 128)
				{
					return (float)(sbyte)base.Data[YAccelerationIndex] * 0.0009765625f;
				}
				return null;
			}
		}

		public bool IsTankLevelAlertActive => (base.Data[TankLevelAlertIndex] & 0x80) == 128;

		public int TankLevelAlertCount => base.Data[TankLevelAlertIndex] & 0x7F;

		public ushort UserMessage => GetUInt16((uint)UserMessageStartIndex);

		public LogicalDeviceTankSensorStatus()
			: this(1)
		{
		}

		public LogicalDeviceTankSensorStatus(int size)
			: base((uint)size, (uint)MaximumStatusPacketSize, 0)
		{
			SetLevel(0);
			SetBatteryLevel(byte.MaxValue);
			SetMeasurementQuality(byte.MaxValue);
			SetXAcceleration(128);
			SetYAcceleration(128);
			SetTankLevelAlert(active: false, 0);
			SetUserMessage(0);
		}

		public LogicalDeviceTankSensorStatus(byte tankLevel, byte batteryLevel, byte measurementQuality, byte xAcceleration, byte yAcceleration)
			: this(5)
		{
			SetLevel(tankLevel);
			SetBatteryLevel(batteryLevel);
			SetMeasurementQuality(measurementQuality);
			SetXAcceleration(xAcceleration);
			SetYAcceleration(yAcceleration);
		}

		public LogicalDeviceTankSensorStatus(byte tankLevel, byte batteryLevel, byte measurementQuality, byte xAcceleration, byte yAcceleration, bool tankLevelActive, byte tankLevelCount, ushort userMessage)
			: this(7)
		{
			SetLevel(tankLevel);
			SetBatteryLevel(batteryLevel);
			SetMeasurementQuality(measurementQuality);
			SetXAcceleration(xAcceleration);
			SetYAcceleration(yAcceleration);
			SetTankLevelAlert(tankLevelActive, tankLevelCount);
			SetUserMessage(userMessage);
		}

		public LogicalDeviceTankSensorStatus(LogicalDeviceTankSensorStatus originalSensorStatus)
			: this()
		{
			byte[] data = originalSensorStatus.Data;
			Update(data, data.Length);
		}

		public LogicalDeviceTankSensorStatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceTankSensorStatusSerializable(this);
		}

		public void SetLevel(byte percent)
		{
			SetByte(127, (byte)MathCommon.Clamp(percent, 0, 100), PercentByteIndex);
		}

		public void SetBatteryLevel(byte percent)
		{
			SetByte(127, (byte)MathCommon.Clamp(percent, 0, 100), BatteryLevelIndex);
		}

		public void SetMeasurementQuality(byte measurementQuality)
		{
			SetByte(127, (byte)MathCommon.Clamp(measurementQuality, 0, 100), MeasurementQualityIndex);
		}

		public void SetXAcceleration(byte xAcceleration)
		{
			SetByte(byte.MaxValue, xAcceleration, XAccelerationIndex);
		}

		public void SetYAcceleration(byte yAcceleration)
		{
			SetByte(byte.MaxValue, yAcceleration, YAccelerationIndex);
		}

		public void SetTankLevelAlert(bool active, byte count)
		{
			byte b = (byte)(count & 0x7Fu);
			if (active)
			{
				b = (byte)(b | 0x80u);
			}
			SetByte(byte.MaxValue, b, TankLevelAlertIndex);
		}

		public void SetUserMessage(ushort userMessage)
		{
			SetUInt16(userMessage, UserMessageStartIndex);
		}

		public override string ToString()
		{
			return $"Device Status: {base.ToString()} Level: {Level} BatteryLevel: {BatteryLevel} " + $"MeasurementQuality: {MeasurementQuality} XAcceleration: {XAcceleration} YAcceleration: {YAcceleration} " + $"TankLevelAlertActive: {IsTankLevelAlertActive} AlertCount: {TankLevelAlertCount} UserMessage: {UserMessage}";
		}
	}
}
