using System;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.TPMS
{
	public class LogicalDeviceTpmsStatusExtended : LogicalDeviceStatusPacketMutableExtended
	{
		public const int MinimumStatusPacketSize = 7;

		public const int TireIndexBitmask = 31;

		public const int GroupIdBitmask = 224;

		private const int ExtendedByteStartIndex = 0;

		private const int PressureStatusIndex = 0;

		private const int TireTempIndex = 1;

		private const int TirePressureIndex = 2;

		private const int RssiIndex = 4;

		private const int SensorDataIndex = 5;

		private const int RotationStatusIndex = 6;

		private const int SensorLowFrequencyTriggerBitmask = 4;

		private const int LowBatteryBitmask = 8;

		private const int SensorLearnedBitmask = 1;

		private const int SensorDataReceivedBitmask = 2;

		private const int SensorMissingBitmask = 4;

		private const int SensorBatteryFaultBitmask = 8;

		private const int SensorRotatingBitmask = 128;

		private const float BatteryVoltageScale = 0.1f;

		private const float BatteryVoltageOffset = 1.9f;

		private const int TirePressureNumberOfBytes = 2;

		private const float TirePressureScale = 0.025f;

		private const float PsiMultiplier = 14.50377f;

		private const int TireTempOffset = 50;

		private const int TireTempMin = -50;

		private const int TireTempMax = 205;

		private static readonly BitPositionValue TireIndexBitPosition = new BitPositionValue(31u);

		private static readonly BitPositionValue GroupIdBitPosition = new BitPositionValue(224u);

		private static readonly BitPositionValue TpmsPressureStatusBitPosition = new BitPositionValue(3u);

		private static readonly BitPositionValue BatteryVoltageBitPosition = new BitPositionValue(240u);

		private static readonly BitPositionValue TirePressureBitPosition = new BitPositionValue(65472u, 2, 2);

		private static readonly BitPositionValue TemperatureFaultBitPosition = new BitPositionValue(48u, 5);

		private static readonly BitPositionValue PressureFaultBitPosition = new BitPositionValue(192u, 5);

		private static readonly BitPositionValue NewSensorRxMessageCountBitPosition = new BitPositionValue(127u, 6);

		public uint TireIndex => TireIndexBitPosition.DecodeValue(ExtendedByte);

		public uint GroupId => GroupIdBitPosition.DecodeValue(ExtendedByte);

		public TpmsPositionalSensorId SensorId => new TpmsPositionalSensorId((TpmsGroupId)GroupId, (byte)TireIndex);

		public TpmsPressureStatus TpmsPressureStatus => (TpmsPressureStatus)GetValue(TpmsPressureStatusBitPosition);

		public bool SensorLowFrequencyTrigger => GetBit(BasicBitMask.BitMask0X04, 0);

		public bool LowBattery => GetBit(BasicBitMask.BitMask0X08, 0);

		public float BatteryVoltage => (float)GetValue(BatteryVoltageBitPosition) * 0.1f + 1.9f;

		public int TireTempCelsius => base.Data[1] - 50;

		public int TireTempFahrenheit => TireTempCelsius * 9 / 5 + 32;

		public float TirePressure => (float)GetValue(TirePressureBitPosition) * 0.025f * 14.50377f;

		public sbyte Rssi => (sbyte)base.Data[4];

		public bool SensorLearned => GetBit(BasicBitMask.BitMask0X01, 5);

		public bool SensorDataReceivedSincePowerOn => GetBit(BasicBitMask.BitMask0X02, 5);

		public bool SensorMissing => GetBit(BasicBitMask.BitMask0X04, 5);

		public bool SensorBatteryFault => GetBit(BasicBitMask.BitMask0X08, 5);

		public TpmsTemperatureFault TpmsTemperatureFault => (TpmsTemperatureFault)GetValue(TemperatureFaultBitPosition);

		public TpmsPressureFault TpmsPressureFault => (TpmsPressureFault)GetValue(PressureFaultBitPosition);

		public bool SensorRotating => GetBit(BasicBitMask.BitMask0X80, 6);

		public byte NewSensorRxMessageCount => (byte)GetValue(NewSensorRxMessageCountBitPosition);

		public LogicalDeviceTpmsStatusExtended()
			: base(7u)
		{
		}

		public LogicalDeviceTpmsStatusExtended(LogicalDeviceTpmsStatusExtended originalStatus)
		{
		}

		public static LogicalDeviceTpmsStatusExtended MakeDefaultExtendedStatus()
		{
			LogicalDeviceTpmsStatusExtended logicalDeviceTpmsStatusExtended = new LogicalDeviceTpmsStatusExtended();
			byte[] array = new byte[7];
			logicalDeviceTpmsStatusExtended.Update(array, array.Length);
			return logicalDeviceTpmsStatusExtended;
		}

		public void SetTireIndex(uint tireIndex)
		{
			ExtendedByte = (byte)TireIndexBitPosition.EncodeValue(tireIndex, ExtendedByte);
		}

		public void SetGroupId(uint groupId)
		{
			ExtendedByte = (byte)GroupIdBitPosition.EncodeValue(groupId, ExtendedByte);
		}

		public void SetTpmsPressureStatus(TpmsPressureStatus pressureStatus)
		{
			SetValue((uint)pressureStatus, TpmsPressureStatusBitPosition);
		}

		public void SetSensorLowFrequencyTrigger(bool enabled)
		{
			SetBit(BasicBitMask.BitMask0X04, enabled, 0);
		}

		public void SetLowBattery(bool lowBattery)
		{
			SetBit(BasicBitMask.BitMask0X08, lowBattery, 0);
		}

		public void SetBatteryVoltage(float batteryVoltage)
		{
			float num = (batteryVoltage - 1.9f) / 0.1f;
			SetValue((uint)num, BatteryVoltageBitPosition);
		}

		public void SetTireTempCelsius(int tireTempCelsius)
		{
			int num = MathCommon.Clamp(tireTempCelsius, -50, 205) + 50;
			SetByte(byte.MaxValue, (byte)num, 1);
		}

		public void SetTirePressure(float pressure)
		{
			float value = pressure / 14.50377f / 0.025f;
			SetValue(Convert.ToUInt32(value), TirePressureBitPosition);
		}

		public void SetRssi(sbyte rssi)
		{
			base.Data[4] = (byte)rssi;
		}

		public void SetSensorConfigured(bool configured)
		{
			SetBit(BasicBitMask.BitMask0X01, configured, 5);
		}

		public void SetSensorDataReceivedSincePowerOn(bool enabled)
		{
			SetBit(BasicBitMask.BitMask0X02, enabled, 5);
		}

		public void SetSensorMissing(bool missing)
		{
			SetBit(BasicBitMask.BitMask0X04, missing, 5);
		}

		public void SetSensorBatteryFault(bool faultDetected)
		{
			SetBit(BasicBitMask.BitMask0X08, faultDetected, 5);
		}

		public void SetTemperatureFault(TpmsTemperatureFault temperatureFault)
		{
			SetValue((uint)temperatureFault, TemperatureFaultBitPosition);
		}

		public void SetPressureFault(TpmsPressureFault pressureFault)
		{
			SetValue((uint)pressureFault, PressureFaultBitPosition);
		}

		public void SetSensorRotating(bool rotating)
		{
			SetBit(BasicBitMask.BitMask0X80, rotating);
		}

		public void SetNewSensorRxMessageCount(int count)
		{
			SetValue((uint)count, NewSensorRxMessageCountBitPosition);
		}

		public override string ToString()
		{
			return $"SensorId: {SensorId} TpmsPressureStatus: {TpmsPressureStatus} " + $"SensorLowFrequencyTrigger: {SensorLowFrequencyTrigger} LowBattery: {LowBattery} " + $"BatteryVoltage: {BatteryVoltage} TireTempCelsius: {TireTempCelsius} TireTempFahrenheit: {TireTempFahrenheit} " + $"TirePressure: {TirePressure} Rssi: {Rssi} SensorLearned: {SensorLearned} " + $"SensorDataReceivedSincePowerOn: {SensorDataReceivedSincePowerOn} SensorMissing: {SensorMissing} " + $"SensorBatteryFault: {SensorBatteryFault} TpmsTempFault: {TpmsTemperatureFault} " + $"TpmsPressureFault: {TpmsPressureFault} SensorRotating: {SensorRotating} " + $"NewSensorRxMessageCount: {NewSensorRxMessageCount} Raw Data: {base.ToString()}";
		}
	}
}
