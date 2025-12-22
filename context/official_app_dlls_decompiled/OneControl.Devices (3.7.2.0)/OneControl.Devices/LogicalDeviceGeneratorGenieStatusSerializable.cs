using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OneControl.Devices
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceGeneratorGenieStatusSerializable : LogicalDeviceStatusSerializableBase<LogicalDeviceGeneratorGenieStatusSerializable>, IEquatable<LogicalDeviceGeneratorGenieStatusSerializable>
	{
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceGeneratorGenieStatusSerializable);
			}
		}

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public GeneratorState State { get; }

		[JsonProperty]
		public bool QuietHoursActive { get; }

		[JsonProperty]
		public float BatteryVoltage { get; }

		[JsonProperty]
		public float TemperatureFahrenheit { get; }

		[JsonConstructor]
		public LogicalDeviceGeneratorGenieStatusSerializable(GeneratorState state, bool quietHoursActive, float batteryVoltage, float temperatureFahrenheit)
		{
			State = state;
			QuietHoursActive = quietHoursActive;
			BatteryVoltage = batteryVoltage;
			TemperatureFahrenheit = temperatureFahrenheit;
		}

		public LogicalDeviceGeneratorGenieStatusSerializable(LogicalDeviceGeneratorGenieStatus status)
			: this(status.State, status.QuietHoursActive, status.BatteryVoltage, status.TemperatureFahrenheit)
		{
		}

		public override byte[] MakeRawData()
		{
			LogicalDeviceGeneratorGenieStatus logicalDeviceGeneratorGenieStatus = new LogicalDeviceGeneratorGenieStatus();
			logicalDeviceGeneratorGenieStatus.SetState(State);
			logicalDeviceGeneratorGenieStatus.SetBatteryVoltage(BatteryVoltage);
			logicalDeviceGeneratorGenieStatus.SetQuietHoursActive(QuietHoursActive);
			logicalDeviceGeneratorGenieStatus.SetTemperature(TemperatureFahrenheit);
			return logicalDeviceGeneratorGenieStatus.CopyCurrentData();
		}

		public static LogicalDeviceGeneratorGenieStatusSerializable MakeStatusSerializable(byte[] rawData)
		{
			int num = rawData.Length;
			LogicalDeviceGeneratorGenieStatus logicalDeviceGeneratorGenieStatus = new LogicalDeviceGeneratorGenieStatus();
			if (num < logicalDeviceGeneratorGenieStatus.MinSize || num > logicalDeviceGeneratorGenieStatus.MaxSize)
			{
				throw new ArgumentOutOfRangeException("rawData", $"Data size must be between {logicalDeviceGeneratorGenieStatus.MinSize} and {logicalDeviceGeneratorGenieStatus.MaxSize}");
			}
			logicalDeviceGeneratorGenieStatus.Update(rawData, rawData.Length);
			return new LogicalDeviceGeneratorGenieStatusSerializable(logicalDeviceGeneratorGenieStatus.State, logicalDeviceGeneratorGenieStatus.QuietHoursActive, logicalDeviceGeneratorGenieStatus.BatteryVoltage, logicalDeviceGeneratorGenieStatus.TemperatureFahrenheit);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceGeneratorGenieStatusSerializable");
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
			builder.Append("State = ");
			builder.Append(State.ToString());
			builder.Append(", QuietHoursActive = ");
			builder.Append(QuietHoursActive.ToString());
			builder.Append(", BatteryVoltage = ");
			builder.Append(BatteryVoltage.ToString());
			builder.Append(", TemperatureFahrenheit = ");
			builder.Append(TemperatureFahrenheit.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceGeneratorGenieStatusSerializable? left, LogicalDeviceGeneratorGenieStatusSerializable? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceGeneratorGenieStatusSerializable? left, LogicalDeviceGeneratorGenieStatusSerializable? right)
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
			return (((base.GetHashCode() * -1521134295 + EqualityComparer<GeneratorState>.Default.GetHashCode(State)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(QuietHoursActive)) * -1521134295 + EqualityComparer<float>.Default.GetHashCode(BatteryVoltage)) * -1521134295 + EqualityComparer<float>.Default.GetHashCode(TemperatureFahrenheit);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceGeneratorGenieStatusSerializable);
		}

		[CompilerGenerated]
		public sealed override bool Equals(LogicalDeviceStatusSerializableBase<LogicalDeviceGeneratorGenieStatusSerializable>? other)
		{
			return Equals((object)other);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceGeneratorGenieStatusSerializable? other)
		{
			if ((object)this != other)
			{
				if (base.Equals(other) && EqualityComparer<GeneratorState>.Default.Equals(State, other!.State) && EqualityComparer<bool>.Default.Equals(QuietHoursActive, other!.QuietHoursActive) && EqualityComparer<float>.Default.Equals(BatteryVoltage, other!.BatteryVoltage))
				{
					return EqualityComparer<float>.Default.Equals(TemperatureFahrenheit, other!.TemperatureFahrenheit);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceGeneratorGenieStatusSerializable(LogicalDeviceGeneratorGenieStatusSerializable original)
			: base((LogicalDeviceStatusSerializableBase<LogicalDeviceGeneratorGenieStatusSerializable>)original)
		{
			State = original.State;
			QuietHoursActive = original.QuietHoursActive;
			BatteryVoltage = original.BatteryVoltage;
			TemperatureFahrenheit = original.TemperatureFahrenheit;
		}
	}
}
