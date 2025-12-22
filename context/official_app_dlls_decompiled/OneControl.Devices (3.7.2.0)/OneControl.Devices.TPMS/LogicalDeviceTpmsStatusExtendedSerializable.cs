using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OneControl.Devices.BatteryMonitor;

namespace OneControl.Devices.TPMS
{
	public class LogicalDeviceTpmsStatusExtendedSerializable : LogicalDeviceStatusExtendedSerializableBase<LogicalDeviceBatteryMonitorStatusExtendedSerializable>, IEquatable<LogicalDeviceTpmsStatusExtendedSerializable>
	{
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceTpmsStatusExtendedSerializable);
			}
		}

		[JsonProperty]
		public byte EnhancedByte { get; }

		[JsonProperty]
		public uint TireIndex { get; }

		[JsonProperty]
		public uint GroupId { get; }

		[JsonProperty]
		public TpmsPositionalSensorId SensorId { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public TpmsPressureStatus TpmsPressureStatus { get; }

		[JsonProperty]
		public bool SensorLowFrequencyTrigger { get; }

		[JsonProperty]
		public bool LowBattery { get; }

		[JsonProperty]
		public float BatteryVoltage { get; }

		[JsonProperty]
		public int TireTempCelsius { get; }

		[JsonProperty]
		public int TireTempFahrenheit { get; }

		[JsonProperty]
		public float TirePressure { get; }

		[JsonProperty]
		public sbyte Rssi { get; }

		[JsonProperty]
		public bool SensorLearned { get; }

		[JsonProperty]
		public bool SensorDataReceivedSincePowerOn { get; }

		[JsonProperty]
		public bool SensorMissing { get; }

		[JsonProperty]
		public bool SensorBatteryFault { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public TpmsTemperatureFault TpmsTemperatureFault { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public TpmsPressureFault TpmsPressureFault { get; }

		[JsonProperty]
		public bool SensorRotating { get; }

		[JsonProperty]
		public byte NewSensorRxMessageCount { get; }

		[JsonConstructor]
		public LogicalDeviceTpmsStatusExtendedSerializable(byte enhancedByte, uint tireIndex, uint groupId, TpmsPositionalSensorId sensorId, TpmsPressureStatus tpmsPressureStatus, bool sensorLowFrequencyTrigger, bool lowBattery, float batteryVoltage, int tireTempCelsius, int tireTempFahrenheit, float tirePressure, sbyte rssi, bool sensorLearned, bool sensorDataReceivedSincePowerOn, bool sensorMissing, bool sensorBatteryFault, TpmsTemperatureFault tpmsTemperatureFault, TpmsPressureFault tpmsPressureFault, bool sensorRotating, byte newSensorRxMessageCount)
		{
			EnhancedByte = enhancedByte;
			TireIndex = tireIndex;
			GroupId = groupId;
			SensorId = sensorId;
			TpmsPressureStatus = tpmsPressureStatus;
			SensorLowFrequencyTrigger = sensorLowFrequencyTrigger;
			LowBattery = lowBattery;
			BatteryVoltage = batteryVoltage;
			TireTempCelsius = tireTempCelsius;
			TireTempFahrenheit = tireTempFahrenheit;
			TirePressure = tirePressure;
			Rssi = rssi;
			SensorLearned = sensorLearned;
			SensorDataReceivedSincePowerOn = sensorDataReceivedSincePowerOn;
			SensorMissing = sensorMissing;
			SensorBatteryFault = sensorBatteryFault;
			TpmsTemperatureFault = tpmsTemperatureFault;
			TpmsPressureFault = tpmsPressureFault;
			SensorRotating = sensorRotating;
			NewSensorRxMessageCount = newSensorRxMessageCount;
		}

		public LogicalDeviceTpmsStatusExtendedSerializable(LogicalDeviceTpmsStatusExtended tpmsStatusExtended)
			: this(tpmsStatusExtended.ExtendedByte, tpmsStatusExtended.TireIndex, tpmsStatusExtended.GroupId, tpmsStatusExtended.SensorId, tpmsStatusExtended.TpmsPressureStatus, tpmsStatusExtended.SensorLowFrequencyTrigger, tpmsStatusExtended.LowBattery, tpmsStatusExtended.BatteryVoltage, tpmsStatusExtended.TireTempCelsius, tpmsStatusExtended.TireTempFahrenheit, tpmsStatusExtended.TirePressure, tpmsStatusExtended.Rssi, tpmsStatusExtended.SensorLearned, tpmsStatusExtended.SensorDataReceivedSincePowerOn, tpmsStatusExtended.SensorMissing, tpmsStatusExtended.SensorBatteryFault, tpmsStatusExtended.TpmsTemperatureFault, tpmsStatusExtended.TpmsPressureFault, tpmsStatusExtended.SensorRotating, tpmsStatusExtended.NewSensorRxMessageCount)
		{
		}

		public override IReadOnlyDictionary<byte, byte[]> MakeRawDataExtended()
		{
			LogicalDeviceTpmsStatusExtended logicalDeviceTpmsStatusExtended = new LogicalDeviceTpmsStatusExtended();
			logicalDeviceTpmsStatusExtended.SetTireIndex((byte)TireIndex);
			logicalDeviceTpmsStatusExtended.SetGroupId((byte)GroupId);
			logicalDeviceTpmsStatusExtended.SetTpmsPressureStatus(TpmsPressureStatus);
			logicalDeviceTpmsStatusExtended.SetSensorLowFrequencyTrigger(SensorLowFrequencyTrigger);
			logicalDeviceTpmsStatusExtended.SetLowBattery(LowBattery);
			logicalDeviceTpmsStatusExtended.SetBatteryVoltage(BatteryVoltage);
			logicalDeviceTpmsStatusExtended.SetTireTempCelsius(TireTempCelsius);
			logicalDeviceTpmsStatusExtended.SetRssi(Rssi);
			logicalDeviceTpmsStatusExtended.SetSensorDataReceivedSincePowerOn(SensorDataReceivedSincePowerOn);
			logicalDeviceTpmsStatusExtended.SetSensorMissing(SensorMissing);
			logicalDeviceTpmsStatusExtended.SetSensorBatteryFault(SensorBatteryFault);
			logicalDeviceTpmsStatusExtended.SetSensorRotating(SensorRotating);
			logicalDeviceTpmsStatusExtended.SetNewSensorRxMessageCount(NewSensorRxMessageCount);
			logicalDeviceTpmsStatusExtended.SetSensorConfigured(SensorLearned);
			logicalDeviceTpmsStatusExtended.SetTemperatureFault(TpmsTemperatureFault);
			logicalDeviceTpmsStatusExtended.SetPressureFault(TpmsPressureFault);
			logicalDeviceTpmsStatusExtended.SetTirePressure(TirePressure);
			return new Dictionary<byte, byte[]> { [EnhancedByte] = logicalDeviceTpmsStatusExtended.CopyCurrentData() };
		}

		public static LogicalDeviceTpmsStatusExtendedSerializable MakeStatusSerializable(byte[] rawData)
		{
			int num = rawData.Length;
			LogicalDeviceTpmsStatusExtended logicalDeviceTpmsStatusExtended = new LogicalDeviceTpmsStatusExtended();
			if (num < logicalDeviceTpmsStatusExtended.MinSize || num > logicalDeviceTpmsStatusExtended.MaxSize)
			{
				throw new ArgumentOutOfRangeException("rawData", $"Data size must be between {logicalDeviceTpmsStatusExtended.MinSize} and {logicalDeviceTpmsStatusExtended.MaxSize}");
			}
			logicalDeviceTpmsStatusExtended.Update(rawData, rawData.Length);
			return new LogicalDeviceTpmsStatusExtendedSerializable(logicalDeviceTpmsStatusExtended);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceTpmsStatusExtendedSerializable");
			stringBuilder.Append(" { ");
			if (PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("EnhancedByte = ");
			builder.Append(EnhancedByte.ToString());
			builder.Append(", TireIndex = ");
			builder.Append(TireIndex.ToString());
			builder.Append(", GroupId = ");
			builder.Append(GroupId.ToString());
			builder.Append(", SensorId = ");
			builder.Append(SensorId.ToString());
			builder.Append(", TpmsPressureStatus = ");
			builder.Append(TpmsPressureStatus.ToString());
			builder.Append(", SensorLowFrequencyTrigger = ");
			builder.Append(SensorLowFrequencyTrigger.ToString());
			builder.Append(", LowBattery = ");
			builder.Append(LowBattery.ToString());
			builder.Append(", BatteryVoltage = ");
			builder.Append(BatteryVoltage.ToString());
			builder.Append(", TireTempCelsius = ");
			builder.Append(TireTempCelsius.ToString());
			builder.Append(", TireTempFahrenheit = ");
			builder.Append(TireTempFahrenheit.ToString());
			builder.Append(", TirePressure = ");
			builder.Append(TirePressure.ToString());
			builder.Append(", Rssi = ");
			builder.Append(Rssi.ToString());
			builder.Append(", SensorLearned = ");
			builder.Append(SensorLearned.ToString());
			builder.Append(", SensorDataReceivedSincePowerOn = ");
			builder.Append(SensorDataReceivedSincePowerOn.ToString());
			builder.Append(", SensorMissing = ");
			builder.Append(SensorMissing.ToString());
			builder.Append(", SensorBatteryFault = ");
			builder.Append(SensorBatteryFault.ToString());
			builder.Append(", TpmsTemperatureFault = ");
			builder.Append(TpmsTemperatureFault.ToString());
			builder.Append(", TpmsPressureFault = ");
			builder.Append(TpmsPressureFault.ToString());
			builder.Append(", SensorRotating = ");
			builder.Append(SensorRotating.ToString());
			builder.Append(", NewSensorRxMessageCount = ");
			builder.Append(NewSensorRxMessageCount.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceTpmsStatusExtendedSerializable? left, LogicalDeviceTpmsStatusExtendedSerializable? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceTpmsStatusExtendedSerializable? left, LogicalDeviceTpmsStatusExtendedSerializable? right)
		{
			if ((object)left != right)
			{
				return left?.Equals(right) ?? false;
			}
			return true;
		}

		[CompilerGenerated]
		public override int GetHashCode()
		{
			return (((((((((((((((((((base.GetHashCode() * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(EnhancedByte)) * -1521134295 + EqualityComparer<uint>.Default.GetHashCode(TireIndex)) * -1521134295 + EqualityComparer<uint>.Default.GetHashCode(GroupId)) * -1521134295 + EqualityComparer<TpmsPositionalSensorId>.Default.GetHashCode(SensorId)) * -1521134295 + EqualityComparer<TpmsPressureStatus>.Default.GetHashCode(TpmsPressureStatus)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(SensorLowFrequencyTrigger)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(LowBattery)) * -1521134295 + EqualityComparer<float>.Default.GetHashCode(BatteryVoltage)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(TireTempCelsius)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(TireTempFahrenheit)) * -1521134295 + EqualityComparer<float>.Default.GetHashCode(TirePressure)) * -1521134295 + EqualityComparer<sbyte>.Default.GetHashCode(Rssi)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(SensorLearned)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(SensorDataReceivedSincePowerOn)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(SensorMissing)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(SensorBatteryFault)) * -1521134295 + EqualityComparer<TpmsTemperatureFault>.Default.GetHashCode(TpmsTemperatureFault)) * -1521134295 + EqualityComparer<TpmsPressureFault>.Default.GetHashCode(TpmsPressureFault)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(SensorRotating)) * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(NewSensorRxMessageCount);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceTpmsStatusExtendedSerializable);
		}

		[CompilerGenerated]
		public sealed override bool Equals(LogicalDeviceStatusExtendedSerializableBase<LogicalDeviceBatteryMonitorStatusExtendedSerializable>? other)
		{
			return Equals((object)other);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceTpmsStatusExtendedSerializable? other)
		{
			if ((object)this != other)
			{
				if (base.Equals(other) && EqualityComparer<byte>.Default.Equals(EnhancedByte, other!.EnhancedByte) && EqualityComparer<uint>.Default.Equals(TireIndex, other!.TireIndex) && EqualityComparer<uint>.Default.Equals(GroupId, other!.GroupId) && EqualityComparer<TpmsPositionalSensorId>.Default.Equals(SensorId, other!.SensorId) && EqualityComparer<TpmsPressureStatus>.Default.Equals(TpmsPressureStatus, other!.TpmsPressureStatus) && EqualityComparer<bool>.Default.Equals(SensorLowFrequencyTrigger, other!.SensorLowFrequencyTrigger) && EqualityComparer<bool>.Default.Equals(LowBattery, other!.LowBattery) && EqualityComparer<float>.Default.Equals(BatteryVoltage, other!.BatteryVoltage) && EqualityComparer<int>.Default.Equals(TireTempCelsius, other!.TireTempCelsius) && EqualityComparer<int>.Default.Equals(TireTempFahrenheit, other!.TireTempFahrenheit) && EqualityComparer<float>.Default.Equals(TirePressure, other!.TirePressure) && EqualityComparer<sbyte>.Default.Equals(Rssi, other!.Rssi) && EqualityComparer<bool>.Default.Equals(SensorLearned, other!.SensorLearned) && EqualityComparer<bool>.Default.Equals(SensorDataReceivedSincePowerOn, other!.SensorDataReceivedSincePowerOn) && EqualityComparer<bool>.Default.Equals(SensorMissing, other!.SensorMissing) && EqualityComparer<bool>.Default.Equals(SensorBatteryFault, other!.SensorBatteryFault) && EqualityComparer<TpmsTemperatureFault>.Default.Equals(TpmsTemperatureFault, other!.TpmsTemperatureFault) && EqualityComparer<TpmsPressureFault>.Default.Equals(TpmsPressureFault, other!.TpmsPressureFault) && EqualityComparer<bool>.Default.Equals(SensorRotating, other!.SensorRotating))
				{
					return EqualityComparer<byte>.Default.Equals(NewSensorRxMessageCount, other!.NewSensorRxMessageCount);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceTpmsStatusExtendedSerializable(LogicalDeviceTpmsStatusExtendedSerializable original)
			: base((LogicalDeviceStatusExtendedSerializableBase<LogicalDeviceBatteryMonitorStatusExtendedSerializable>)original)
		{
			EnhancedByte = original.EnhancedByte;
			TireIndex = original.TireIndex;
			GroupId = original.GroupId;
			SensorId = original.SensorId;
			TpmsPressureStatus = original.TpmsPressureStatus;
			SensorLowFrequencyTrigger = original.SensorLowFrequencyTrigger;
			LowBattery = original.LowBattery;
			BatteryVoltage = original.BatteryVoltage;
			TireTempCelsius = original.TireTempCelsius;
			TireTempFahrenheit = original.TireTempFahrenheit;
			TirePressure = original.TirePressure;
			Rssi = original.Rssi;
			SensorLearned = original.SensorLearned;
			SensorDataReceivedSincePowerOn = original.SensorDataReceivedSincePowerOn;
			SensorMissing = original.SensorMissing;
			SensorBatteryFault = original.SensorBatteryFault;
			TpmsTemperatureFault = original.TpmsTemperatureFault;
			TpmsPressureFault = original.TpmsPressureFault;
			SensorRotating = original.SensorRotating;
			NewSensorRxMessageCount = original.NewSensorRxMessageCount;
		}
	}
}
