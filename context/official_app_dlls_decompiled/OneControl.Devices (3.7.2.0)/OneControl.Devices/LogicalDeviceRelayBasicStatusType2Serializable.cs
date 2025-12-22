using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicStatusType2Serializable : LogicalDeviceStatusSerializableBase<LogicalDeviceRelayBasicStatusType2Serializable>, ILogicalDeviceRelayBasicStatusSerializable, ILogicalDeviceStatusSerializable, IJsonSerializerClass, IEquatable<LogicalDeviceRelayBasicStatusType2Serializable>
	{
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceRelayBasicStatusType2Serializable);
			}
		}

		[JsonProperty]
		public bool IsFaulted { get; }

		[JsonProperty]
		public bool IsOn { get; }

		[JsonProperty]
		public bool UserClearRequired { get; }

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public DTC_ID UserMessageDtc { get; }

		[JsonProperty]
		public bool CommandOnAllowed { get; }

		[JsonProperty]
		public byte Position { get; }

		[JsonProperty]
		public float CurrentDrawAmps { get; }

		[JsonConstructor]
		public LogicalDeviceRelayBasicStatusType2Serializable(bool isFaulted, bool isOn, bool userClearRequired, DTC_ID userMessageDtc, bool commandOnAllowed, byte position, float currentDrawAmps)
		{
			IsFaulted = isFaulted;
			IsOn = isOn;
			UserClearRequired = userClearRequired;
			UserMessageDtc = userMessageDtc;
			CommandOnAllowed = commandOnAllowed;
			Position = position;
			CurrentDrawAmps = currentDrawAmps;
		}

		public LogicalDeviceRelayBasicStatusType2Serializable(ILogicalDeviceRelayBasicStatus status)
			: this(status.IsFaulted, status.IsOn, status.UserClearRequired, status.UserMessageDtc, status.CommandOnAllowed, status.Position, status.CurrentDrawAmps)
		{
		}

		public override byte[] MakeRawData()
		{
			LogicalDeviceRelayBasicStatusType2 logicalDeviceRelayBasicStatusType = new LogicalDeviceRelayBasicStatusType2();
			logicalDeviceRelayBasicStatusType.SetState(IsOn);
			logicalDeviceRelayBasicStatusType.SetFault(IsFaulted);
			logicalDeviceRelayBasicStatusType.SetUserClearRequired(UserClearRequired);
			logicalDeviceRelayBasicStatusType.SetCommandOnAllowed(CommandOnAllowed);
			logicalDeviceRelayBasicStatusType.SetPosition(Position);
			logicalDeviceRelayBasicStatusType.SetCurrentDrawAmps(CurrentDrawAmps);
			return logicalDeviceRelayBasicStatusType.CopyCurrentData();
		}

		public static LogicalDeviceRelayBasicStatusType2Serializable MakeStatusSerializable(byte[] rawData)
		{
			int num = rawData.Length;
			LogicalDeviceRelayBasicStatusType2 logicalDeviceRelayBasicStatusType = new LogicalDeviceRelayBasicStatusType2();
			if (num < logicalDeviceRelayBasicStatusType.MinSize || num > logicalDeviceRelayBasicStatusType.MaxSize)
			{
				throw new ArgumentOutOfRangeException("rawData", $"Data size must be between {logicalDeviceRelayBasicStatusType.MinSize} and {logicalDeviceRelayBasicStatusType.MaxSize}");
			}
			logicalDeviceRelayBasicStatusType.Update(rawData, rawData.Length);
			return new LogicalDeviceRelayBasicStatusType2Serializable(logicalDeviceRelayBasicStatusType);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceRelayBasicStatusType2Serializable");
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
			builder.Append("IsFaulted = ");
			builder.Append(IsFaulted.ToString());
			builder.Append(", IsOn = ");
			builder.Append(IsOn.ToString());
			builder.Append(", UserClearRequired = ");
			builder.Append(UserClearRequired.ToString());
			builder.Append(", UserMessageDtc = ");
			builder.Append(UserMessageDtc.ToString());
			builder.Append(", CommandOnAllowed = ");
			builder.Append(CommandOnAllowed.ToString());
			builder.Append(", Position = ");
			builder.Append(Position.ToString());
			builder.Append(", CurrentDrawAmps = ");
			builder.Append(CurrentDrawAmps.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceRelayBasicStatusType2Serializable? left, LogicalDeviceRelayBasicStatusType2Serializable? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceRelayBasicStatusType2Serializable? left, LogicalDeviceRelayBasicStatusType2Serializable? right)
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
			return ((((((base.GetHashCode() * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsFaulted)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsOn)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(UserClearRequired)) * -1521134295 + EqualityComparer<DTC_ID>.Default.GetHashCode(UserMessageDtc)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(CommandOnAllowed)) * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(Position)) * -1521134295 + EqualityComparer<float>.Default.GetHashCode(CurrentDrawAmps);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceRelayBasicStatusType2Serializable);
		}

		[CompilerGenerated]
		public sealed override bool Equals(LogicalDeviceStatusSerializableBase<LogicalDeviceRelayBasicStatusType2Serializable>? other)
		{
			return Equals((object)other);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceRelayBasicStatusType2Serializable? other)
		{
			if ((object)this != other)
			{
				if (base.Equals(other) && EqualityComparer<bool>.Default.Equals(IsFaulted, other!.IsFaulted) && EqualityComparer<bool>.Default.Equals(IsOn, other!.IsOn) && EqualityComparer<bool>.Default.Equals(UserClearRequired, other!.UserClearRequired) && EqualityComparer<DTC_ID>.Default.Equals(UserMessageDtc, other!.UserMessageDtc) && EqualityComparer<bool>.Default.Equals(CommandOnAllowed, other!.CommandOnAllowed) && EqualityComparer<byte>.Default.Equals(Position, other!.Position))
				{
					return EqualityComparer<float>.Default.Equals(CurrentDrawAmps, other!.CurrentDrawAmps);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceRelayBasicStatusType2Serializable(LogicalDeviceRelayBasicStatusType2Serializable original)
			: base((LogicalDeviceStatusSerializableBase<LogicalDeviceRelayBasicStatusType2Serializable>)original)
		{
			IsFaulted = original.IsFaulted;
			IsOn = original.IsOn;
			UserClearRequired = original.UserClearRequired;
			UserMessageDtc = original.UserMessageDtc;
			CommandOnAllowed = original.CommandOnAllowed;
			Position = original.Position;
			CurrentDrawAmps = original.CurrentDrawAmps;
		}
	}
}
