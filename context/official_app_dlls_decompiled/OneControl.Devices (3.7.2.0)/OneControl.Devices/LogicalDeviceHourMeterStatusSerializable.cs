using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Devices
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceHourMeterStatusSerializable : LogicalDeviceStatusSerializableBase<LogicalDeviceHourMeterStatusSerializable>, IReadOnlyLogicalDeviceHourMeterStatus, IEquatable<LogicalDeviceHourMeterStatusSerializable>
	{
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceHourMeterStatusSerializable);
			}
		}

		[JsonProperty]
		public bool Running { get; }

		[JsonProperty]
		public bool MaintenanceDue { get; }

		[JsonProperty]
		public bool MaintenancePastDue { get; }

		[JsonProperty]
		public bool Stopping { get; }

		[JsonProperty]
		public bool Starting { get; }

		[JsonProperty]
		public bool Error { get; }

		[JsonProperty]
		public bool IsHourMeterValid { get; }

		[JsonProperty]
		public uint OperatingSeconds { get; }

		[JsonConstructor]
		public LogicalDeviceHourMeterStatusSerializable(bool running, bool maintenanceDue, bool maintenancePastDue, bool stopping, bool starting, bool error, bool isHourMeterValid, uint operatingSeconds)
		{
			Running = running;
			MaintenanceDue = maintenanceDue;
			MaintenancePastDue = maintenancePastDue;
			Stopping = stopping;
			Starting = starting;
			Error = error;
			IsHourMeterValid = isHourMeterValid;
			OperatingSeconds = operatingSeconds;
		}

		public LogicalDeviceHourMeterStatusSerializable(LogicalDeviceHourMeterStatus status)
			: this(status.Running, status.MaintenanceDue, status.MaintenancePastDue, status.Stopping, status.Starting, status.Error, status.IsHourMeterValid, status.OperatingSeconds)
		{
		}

		public override byte[] MakeRawData()
		{
			LogicalDeviceHourMeterStatus logicalDeviceHourMeterStatus = new LogicalDeviceHourMeterStatus();
			logicalDeviceHourMeterStatus.SetRunning(Running);
			logicalDeviceHourMeterStatus.SetMaintenanceDue(MaintenanceDue);
			logicalDeviceHourMeterStatus.SetStopping(Stopping);
			logicalDeviceHourMeterStatus.SetStarting(Starting);
			logicalDeviceHourMeterStatus.SetError(Error);
			logicalDeviceHourMeterStatus.SetHourMeterValid(IsHourMeterValid);
			logicalDeviceHourMeterStatus.SetOperatingSeconds(OperatingSeconds);
			return logicalDeviceHourMeterStatus.CopyCurrentData();
		}

		public static LogicalDeviceHourMeterStatusSerializable MakeStatusSerializable(byte[] rawData)
		{
			int num = rawData.Length;
			LogicalDeviceHourMeterStatus logicalDeviceHourMeterStatus = new LogicalDeviceHourMeterStatus();
			if (num < logicalDeviceHourMeterStatus.MinSize || num > logicalDeviceHourMeterStatus.MaxSize)
			{
				throw new ArgumentOutOfRangeException("rawData", $"Data size must be between {logicalDeviceHourMeterStatus.MinSize} and {logicalDeviceHourMeterStatus.MaxSize}");
			}
			logicalDeviceHourMeterStatus.Update(rawData, rawData.Length);
			return new LogicalDeviceHourMeterStatusSerializable(logicalDeviceHourMeterStatus.Running, logicalDeviceHourMeterStatus.MaintenanceDue, logicalDeviceHourMeterStatus.MaintenancePastDue, logicalDeviceHourMeterStatus.Stopping, logicalDeviceHourMeterStatus.Starting, logicalDeviceHourMeterStatus.Error, logicalDeviceHourMeterStatus.IsHourMeterValid, logicalDeviceHourMeterStatus.OperatingSeconds);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceHourMeterStatusSerializable");
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
			builder.Append("Running = ");
			builder.Append(Running.ToString());
			builder.Append(", MaintenanceDue = ");
			builder.Append(MaintenanceDue.ToString());
			builder.Append(", MaintenancePastDue = ");
			builder.Append(MaintenancePastDue.ToString());
			builder.Append(", Stopping = ");
			builder.Append(Stopping.ToString());
			builder.Append(", Starting = ");
			builder.Append(Starting.ToString());
			builder.Append(", Error = ");
			builder.Append(Error.ToString());
			builder.Append(", IsHourMeterValid = ");
			builder.Append(IsHourMeterValid.ToString());
			builder.Append(", OperatingSeconds = ");
			builder.Append(OperatingSeconds.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceHourMeterStatusSerializable? left, LogicalDeviceHourMeterStatusSerializable? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceHourMeterStatusSerializable? left, LogicalDeviceHourMeterStatusSerializable? right)
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
			return (((((((base.GetHashCode() * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(Running)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(MaintenanceDue)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(MaintenancePastDue)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(Stopping)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(Starting)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(Error)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsHourMeterValid)) * -1521134295 + EqualityComparer<uint>.Default.GetHashCode(OperatingSeconds);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceHourMeterStatusSerializable);
		}

		[CompilerGenerated]
		public sealed override bool Equals(LogicalDeviceStatusSerializableBase<LogicalDeviceHourMeterStatusSerializable>? other)
		{
			return Equals((object)other);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceHourMeterStatusSerializable? other)
		{
			if ((object)this != other)
			{
				if (base.Equals(other) && EqualityComparer<bool>.Default.Equals(Running, other!.Running) && EqualityComparer<bool>.Default.Equals(MaintenanceDue, other!.MaintenanceDue) && EqualityComparer<bool>.Default.Equals(MaintenancePastDue, other!.MaintenancePastDue) && EqualityComparer<bool>.Default.Equals(Stopping, other!.Stopping) && EqualityComparer<bool>.Default.Equals(Starting, other!.Starting) && EqualityComparer<bool>.Default.Equals(Error, other!.Error) && EqualityComparer<bool>.Default.Equals(IsHourMeterValid, other!.IsHourMeterValid))
				{
					return EqualityComparer<uint>.Default.Equals(OperatingSeconds, other!.OperatingSeconds);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceHourMeterStatusSerializable(LogicalDeviceHourMeterStatusSerializable original)
			: base((LogicalDeviceStatusSerializableBase<LogicalDeviceHourMeterStatusSerializable>)original)
		{
			Running = original.Running;
			MaintenanceDue = original.MaintenanceDue;
			MaintenancePastDue = original.MaintenancePastDue;
			Stopping = original.Stopping;
			Starting = original.Starting;
			Error = original.Error;
			IsHourMeterValid = original.IsHourMeterValid;
			OperatingSeconds = original.OperatingSeconds;
		}
	}
}
