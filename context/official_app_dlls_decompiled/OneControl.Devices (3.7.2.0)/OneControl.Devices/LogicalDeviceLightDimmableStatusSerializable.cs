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
	public class LogicalDeviceLightDimmableStatusSerializable : LogicalDeviceStatusSerializableBase<LogicalDeviceLightDimmableStatusSerializable>, IEquatable<LogicalDeviceLightDimmableStatusSerializable>
	{
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceLightDimmableStatusSerializable);
			}
		}

		[JsonProperty]
		public bool IsOn { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public DimmableLightMode Mode { get; }

		[JsonProperty]
		public byte MaxBrightness { get; }

		[JsonProperty]
		public byte Duration { get; }

		[JsonProperty]
		public byte Brightness { get; }

		[JsonProperty]
		public int CycleTime1 { get; }

		[JsonProperty]
		public int CycleTime2 { get; }

		[JsonConstructor]
		public LogicalDeviceLightDimmableStatusSerializable(bool isOn, DimmableLightMode mode, byte maxBrightness, byte duration, byte brightness, int cycleTime1, int cycleTime2)
		{
			IsOn = isOn;
			Mode = mode;
			MaxBrightness = maxBrightness;
			Duration = duration;
			Brightness = brightness;
			CycleTime1 = cycleTime1;
			CycleTime2 = cycleTime2;
		}

		public LogicalDeviceLightDimmableStatusSerializable(LogicalDeviceLightDimmableStatus status)
			: this(status.On, status.Mode, status.MaxBrightness, status.Duration, status.Brightness, status.CycleTime1, status.CycleTime2)
		{
		}

		public override byte[] MakeRawData()
		{
			LogicalDeviceLightDimmableStatus logicalDeviceLightDimmableStatus = new LogicalDeviceLightDimmableStatus();
			logicalDeviceLightDimmableStatus.SetLightMode(Mode);
			logicalDeviceLightDimmableStatus.SetBrightness(MaxBrightness);
			logicalDeviceLightDimmableStatus.SetDuration(Duration);
			logicalDeviceLightDimmableStatus.SetBrightness(Brightness);
			logicalDeviceLightDimmableStatus.SetCycleTime1(CycleTime1);
			logicalDeviceLightDimmableStatus.SetCycleTime2(CycleTime2);
			return logicalDeviceLightDimmableStatus.CopyCurrentData();
		}

		public static LogicalDeviceLightDimmableStatusSerializable MakeStatusSerializable(byte[] rawData)
		{
			int num = rawData.Length;
			LogicalDeviceLightDimmableStatus logicalDeviceLightDimmableStatus = new LogicalDeviceLightDimmableStatus();
			if (num < logicalDeviceLightDimmableStatus.MinSize || num > logicalDeviceLightDimmableStatus.MaxSize)
			{
				throw new ArgumentOutOfRangeException("rawData", $"Data size must be between {logicalDeviceLightDimmableStatus.MinSize} and {logicalDeviceLightDimmableStatus.MaxSize}");
			}
			logicalDeviceLightDimmableStatus.Update(rawData, rawData.Length);
			return new LogicalDeviceLightDimmableStatusSerializable(logicalDeviceLightDimmableStatus);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceLightDimmableStatusSerializable");
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
			builder.Append("IsOn = ");
			builder.Append(IsOn.ToString());
			builder.Append(", Mode = ");
			builder.Append(Mode.ToString());
			builder.Append(", MaxBrightness = ");
			builder.Append(MaxBrightness.ToString());
			builder.Append(", Duration = ");
			builder.Append(Duration.ToString());
			builder.Append(", Brightness = ");
			builder.Append(Brightness.ToString());
			builder.Append(", CycleTime1 = ");
			builder.Append(CycleTime1.ToString());
			builder.Append(", CycleTime2 = ");
			builder.Append(CycleTime2.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceLightDimmableStatusSerializable? left, LogicalDeviceLightDimmableStatusSerializable? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceLightDimmableStatusSerializable? left, LogicalDeviceLightDimmableStatusSerializable? right)
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
			return ((((((base.GetHashCode() * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsOn)) * -1521134295 + EqualityComparer<DimmableLightMode>.Default.GetHashCode(Mode)) * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(MaxBrightness)) * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(Duration)) * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(Brightness)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(CycleTime1)) * -1521134295 + EqualityComparer<int>.Default.GetHashCode(CycleTime2);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceLightDimmableStatusSerializable);
		}

		[CompilerGenerated]
		public sealed override bool Equals(LogicalDeviceStatusSerializableBase<LogicalDeviceLightDimmableStatusSerializable>? other)
		{
			return Equals((object)other);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceLightDimmableStatusSerializable? other)
		{
			if ((object)this != other)
			{
				if (base.Equals(other) && EqualityComparer<bool>.Default.Equals(IsOn, other!.IsOn) && EqualityComparer<DimmableLightMode>.Default.Equals(Mode, other!.Mode) && EqualityComparer<byte>.Default.Equals(MaxBrightness, other!.MaxBrightness) && EqualityComparer<byte>.Default.Equals(Duration, other!.Duration) && EqualityComparer<byte>.Default.Equals(Brightness, other!.Brightness) && EqualityComparer<int>.Default.Equals(CycleTime1, other!.CycleTime1))
				{
					return EqualityComparer<int>.Default.Equals(CycleTime2, other!.CycleTime2);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceLightDimmableStatusSerializable(LogicalDeviceLightDimmableStatusSerializable original)
			: base((LogicalDeviceStatusSerializableBase<LogicalDeviceLightDimmableStatusSerializable>)original)
		{
			IsOn = original.IsOn;
			Mode = original.Mode;
			MaxBrightness = original.MaxBrightness;
			Duration = original.Duration;
			Brightness = original.Brightness;
			CycleTime1 = original.CycleTime1;
			CycleTime2 = original.CycleTime2;
		}
	}
}
