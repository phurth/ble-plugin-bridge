using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OneControl.Devices.TPMS;

namespace OneControl.Devices
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTpmsStatusSerializable : LogicalDeviceStatusSerializableBase<LogicalDeviceTpmsStatusSerializable>, IEquatable<LogicalDeviceTpmsStatusSerializable>
	{
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceTpmsStatusSerializable);
			}
		}

		[JsonProperty]
		public byte LearnedSensorCount { get; }

		[JsonProperty]
		public byte VehicleConfigSequence { get; }

		[JsonProperty]
		public byte SensorConfigSequence { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public TpmsRepeaterMode TpmsRepeaterMode { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public TpmsRepeaterVoltageLevel TpmsRepeaterVoltageLevel { get; }

		[JsonProperty]
		public bool RepeaterBatteryLow { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public TpmsRepeaterPowerSource RepeaterPowerSource { get; }

		[JsonProperty]
		public bool RepeaterHasHardwareFault { get; }

		[JsonProperty]
		public bool RepeaterCharging { get; }

		[JsonProperty]
		public bool RepeaterHasBattery { get; }

		public LogicalDeviceTpmsStatusSerializable(LogicalDeviceTpmsStatus status)
			: this(status.LearnedSensorCount, status.VehicleConfigSequence, status.SensorConfigSequence, status.TpmsRepeaterMode, status.TpmsRepeaterVoltageLevel, status.RepeaterBatteryLow, status.RepeaterPowerSource, status.RepeaterHasHardwareFault, status.RepeaterCharging, status.RepeaterHasBattery)
		{
		}

		[JsonConstructor]
		public LogicalDeviceTpmsStatusSerializable(byte learnedSensorCount, byte vehicleConfigSequence, byte sensorConfigSequence, TpmsRepeaterMode tpmsRepeaterMode, TpmsRepeaterVoltageLevel tpmsRepeaterVoltageLevel, bool repeaterBatteryLow, TpmsRepeaterPowerSource repeaterPowerSource, bool repeaterHasHardwareFault, bool repeaterCharging, bool repeaterHasBattery)
		{
			LearnedSensorCount = learnedSensorCount;
			VehicleConfigSequence = vehicleConfigSequence;
			SensorConfigSequence = sensorConfigSequence;
			TpmsRepeaterMode = tpmsRepeaterMode;
			TpmsRepeaterVoltageLevel = tpmsRepeaterVoltageLevel;
			RepeaterBatteryLow = repeaterBatteryLow;
			RepeaterPowerSource = repeaterPowerSource;
			RepeaterHasHardwareFault = repeaterHasHardwareFault;
			RepeaterCharging = repeaterCharging;
			RepeaterHasBattery = repeaterHasBattery;
		}

		public override byte[] MakeRawData()
		{
			LogicalDeviceTpmsStatus logicalDeviceTpmsStatus = new LogicalDeviceTpmsStatus();
			logicalDeviceTpmsStatus.SetRepeaterVoltageLevel(TpmsRepeaterVoltageLevel);
			logicalDeviceTpmsStatus.SetRepeaterPowerSource(RepeaterPowerSource);
			logicalDeviceTpmsStatus.SetSensorCount(LearnedSensorCount);
			logicalDeviceTpmsStatus.SetVehicleConfigSequence(VehicleConfigSequence);
			logicalDeviceTpmsStatus.SetSensorConfigSequence(SensorConfigSequence);
			logicalDeviceTpmsStatus.SetRepeaterMode(TpmsRepeaterMode);
			logicalDeviceTpmsStatus.SetRepeaterBatteryLow(RepeaterBatteryLow);
			logicalDeviceTpmsStatus.SetRepeaterHasHardwareFault(RepeaterHasHardwareFault);
			logicalDeviceTpmsStatus.SetRepeaterCharging(RepeaterCharging);
			logicalDeviceTpmsStatus.SetRepeaterHasBattery(RepeaterHasBattery);
			return logicalDeviceTpmsStatus.CopyCurrentData();
		}

		public static LogicalDeviceTpmsStatusSerializable MakeStatusSerializable(byte[] rawData)
		{
			int num = rawData.Length;
			LogicalDeviceTpmsStatus logicalDeviceTpmsStatus = new LogicalDeviceTpmsStatus();
			if (num < logicalDeviceTpmsStatus.MinSize || num > logicalDeviceTpmsStatus.MaxSize)
			{
				throw new ArgumentOutOfRangeException("rawData", $"Data size must be between {logicalDeviceTpmsStatus.MinSize} and {logicalDeviceTpmsStatus.MaxSize}");
			}
			logicalDeviceTpmsStatus.Update(rawData, rawData.Length);
			return new LogicalDeviceTpmsStatusSerializable(logicalDeviceTpmsStatus);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceTpmsStatusSerializable");
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
			builder.Append("LearnedSensorCount = ");
			builder.Append(LearnedSensorCount.ToString());
			builder.Append(", VehicleConfigSequence = ");
			builder.Append(VehicleConfigSequence.ToString());
			builder.Append(", SensorConfigSequence = ");
			builder.Append(SensorConfigSequence.ToString());
			builder.Append(", TpmsRepeaterMode = ");
			builder.Append(TpmsRepeaterMode.ToString());
			builder.Append(", TpmsRepeaterVoltageLevel = ");
			builder.Append(TpmsRepeaterVoltageLevel.ToString());
			builder.Append(", RepeaterBatteryLow = ");
			builder.Append(RepeaterBatteryLow.ToString());
			builder.Append(", RepeaterPowerSource = ");
			builder.Append(RepeaterPowerSource.ToString());
			builder.Append(", RepeaterHasHardwareFault = ");
			builder.Append(RepeaterHasHardwareFault.ToString());
			builder.Append(", RepeaterCharging = ");
			builder.Append(RepeaterCharging.ToString());
			builder.Append(", RepeaterHasBattery = ");
			builder.Append(RepeaterHasBattery.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceTpmsStatusSerializable? left, LogicalDeviceTpmsStatusSerializable? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceTpmsStatusSerializable? left, LogicalDeviceTpmsStatusSerializable? right)
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
			return (((((((((base.GetHashCode() * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(LearnedSensorCount)) * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(VehicleConfigSequence)) * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(SensorConfigSequence)) * -1521134295 + EqualityComparer<TpmsRepeaterMode>.Default.GetHashCode(TpmsRepeaterMode)) * -1521134295 + EqualityComparer<TpmsRepeaterVoltageLevel>.Default.GetHashCode(TpmsRepeaterVoltageLevel)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(RepeaterBatteryLow)) * -1521134295 + EqualityComparer<TpmsRepeaterPowerSource>.Default.GetHashCode(RepeaterPowerSource)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(RepeaterHasHardwareFault)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(RepeaterCharging)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(RepeaterHasBattery);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceTpmsStatusSerializable);
		}

		[CompilerGenerated]
		public sealed override bool Equals(LogicalDeviceStatusSerializableBase<LogicalDeviceTpmsStatusSerializable>? other)
		{
			return Equals((object)other);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceTpmsStatusSerializable? other)
		{
			if ((object)this != other)
			{
				if (base.Equals(other) && EqualityComparer<byte>.Default.Equals(LearnedSensorCount, other!.LearnedSensorCount) && EqualityComparer<byte>.Default.Equals(VehicleConfigSequence, other!.VehicleConfigSequence) && EqualityComparer<byte>.Default.Equals(SensorConfigSequence, other!.SensorConfigSequence) && EqualityComparer<TpmsRepeaterMode>.Default.Equals(TpmsRepeaterMode, other!.TpmsRepeaterMode) && EqualityComparer<TpmsRepeaterVoltageLevel>.Default.Equals(TpmsRepeaterVoltageLevel, other!.TpmsRepeaterVoltageLevel) && EqualityComparer<bool>.Default.Equals(RepeaterBatteryLow, other!.RepeaterBatteryLow) && EqualityComparer<TpmsRepeaterPowerSource>.Default.Equals(RepeaterPowerSource, other!.RepeaterPowerSource) && EqualityComparer<bool>.Default.Equals(RepeaterHasHardwareFault, other!.RepeaterHasHardwareFault) && EqualityComparer<bool>.Default.Equals(RepeaterCharging, other!.RepeaterCharging))
				{
					return EqualityComparer<bool>.Default.Equals(RepeaterHasBattery, other!.RepeaterHasBattery);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceTpmsStatusSerializable(LogicalDeviceTpmsStatusSerializable original)
			: base((LogicalDeviceStatusSerializableBase<LogicalDeviceTpmsStatusSerializable>)original)
		{
			LearnedSensorCount = original.LearnedSensorCount;
			VehicleConfigSequence = original.VehicleConfigSequence;
			SensorConfigSequence = original.SensorConfigSequence;
			TpmsRepeaterMode = original.TpmsRepeaterMode;
			TpmsRepeaterVoltageLevel = original.TpmsRepeaterVoltageLevel;
			RepeaterBatteryLow = original.RepeaterBatteryLow;
			RepeaterPowerSource = original.RepeaterPowerSource;
			RepeaterHasHardwareFault = original.RepeaterHasHardwareFault;
			RepeaterCharging = original.RepeaterCharging;
			RepeaterHasBattery = original.RepeaterHasBattery;
		}
	}
}
