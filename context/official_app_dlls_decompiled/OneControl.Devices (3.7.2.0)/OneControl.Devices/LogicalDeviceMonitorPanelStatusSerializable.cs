using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Devices
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceMonitorPanelStatusSerializable : LogicalDeviceStatusSerializableBase<LogicalDeviceMonitorPanelStatusSerializable>, IEquatable<LogicalDeviceMonitorPanelStatusSerializable>
	{
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceMonitorPanelStatusSerializable);
			}
		}

		[JsonProperty]
		public byte StatusFlags { get; }

		[JsonConstructor]
		public LogicalDeviceMonitorPanelStatusSerializable(byte statusFlags)
		{
			StatusFlags = statusFlags;
		}

		public LogicalDeviceMonitorPanelStatusSerializable(LogicalDeviceMonitorPanelStatus status)
			: this(status.StatusRaw)
		{
		}

		public override byte[] MakeRawData()
		{
			LogicalDeviceMonitorPanelStatus logicalDeviceMonitorPanelStatus = new LogicalDeviceMonitorPanelStatus();
			logicalDeviceMonitorPanelStatus.SetStatusRaw(StatusFlags);
			return logicalDeviceMonitorPanelStatus.CopyCurrentData();
		}

		public static LogicalDeviceMonitorPanelStatusSerializable MakeStatusSerializable(byte[] rawData)
		{
			int num = rawData.Length;
			LogicalDeviceMonitorPanelStatus logicalDeviceMonitorPanelStatus = new LogicalDeviceMonitorPanelStatus();
			if (num < logicalDeviceMonitorPanelStatus.MinSize || num > logicalDeviceMonitorPanelStatus.MaxSize)
			{
				throw new ArgumentOutOfRangeException("rawData", $"Data size must be between {logicalDeviceMonitorPanelStatus.MinSize} and {logicalDeviceMonitorPanelStatus.MaxSize}");
			}
			logicalDeviceMonitorPanelStatus.Update(rawData, rawData.Length);
			return new LogicalDeviceMonitorPanelStatusSerializable(logicalDeviceMonitorPanelStatus.StatusRaw);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceMonitorPanelStatusSerializable");
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
			builder.Append("StatusFlags = ");
			builder.Append(StatusFlags.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceMonitorPanelStatusSerializable? left, LogicalDeviceMonitorPanelStatusSerializable? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceMonitorPanelStatusSerializable? left, LogicalDeviceMonitorPanelStatusSerializable? right)
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
			return base.GetHashCode() * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(StatusFlags);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceMonitorPanelStatusSerializable);
		}

		[CompilerGenerated]
		public sealed override bool Equals(LogicalDeviceStatusSerializableBase<LogicalDeviceMonitorPanelStatusSerializable>? other)
		{
			return Equals((object)other);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceMonitorPanelStatusSerializable? other)
		{
			if ((object)this != other)
			{
				if (base.Equals(other))
				{
					return EqualityComparer<byte>.Default.Equals(StatusFlags, other!.StatusFlags);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceMonitorPanelStatusSerializable(LogicalDeviceMonitorPanelStatusSerializable original)
			: base((LogicalDeviceStatusSerializableBase<LogicalDeviceMonitorPanelStatusSerializable>)original)
		{
			StatusFlags = original.StatusFlags;
		}
	}
}
