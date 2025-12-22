using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OneControl.Devices.RelayHBridge.Type2
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceRelayHBridgeStatusType2Serializable : LogicalDeviceStatusSerializableBase<LogicalDeviceRelayHBridgeStatusType2Serializable>, IEquatable<LogicalDeviceRelayHBridgeStatusType2Serializable>
	{
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceRelayHBridgeStatusType2Serializable);
			}
		}

		[JsonProperty]
		[JsonConverter(typeof(StringEnumConverter))]
		public RelayHBridgeDirection State { get; }

		[JsonProperty]
		public bool UserClearRequired { get; }

		[JsonProperty]
		public bool ReverseCommandNotHazardous { get; }

		[JsonProperty]
		public bool ForwardCommandNotHazardous { get; }

		[JsonProperty]
		public byte Position { get; }

		[JsonProperty]
		public float CurrentDrawAmps { get; }

		[JsonProperty]
		[JsonConverter(typeof(DTC_ID_JsonConverter))]
		public DTC_ID UserMessageDtc { get; }

		[JsonConstructor]
		public LogicalDeviceRelayHBridgeStatusType2Serializable(RelayHBridgeDirection state, bool userClearRequired, bool reverseCommandNotHazardous, bool forwardCommandNotHazardous, byte position, float currentDrawAmps, DTC_ID userMessageDtc)
		{
			State = state;
			UserClearRequired = userClearRequired;
			ReverseCommandNotHazardous = reverseCommandNotHazardous;
			ForwardCommandNotHazardous = forwardCommandNotHazardous;
			Position = position;
			CurrentDrawAmps = currentDrawAmps;
			UserMessageDtc = userMessageDtc;
		}

		public LogicalDeviceRelayHBridgeStatusType2Serializable(LogicalDeviceRelayHBridgeStatusType2 relayStatus)
			: this(relayStatus.State, relayStatus.UserClearRequired, relayStatus.CommandReverseNotHazardous, relayStatus.CommandForwardNotHazardous, relayStatus.Position, relayStatus.CurrentDrawAmps, relayStatus.UserMessageDtc)
		{
		}

		public override byte[] MakeRawData()
		{
			LogicalDeviceRelayHBridgeStatusType2 logicalDeviceRelayHBridgeStatusType = new LogicalDeviceRelayHBridgeStatusType2();
			logicalDeviceRelayHBridgeStatusType.SetState(State);
			logicalDeviceRelayHBridgeStatusType.SetUserClearRequired(UserClearRequired);
			logicalDeviceRelayHBridgeStatusType.SetCommandReverseNotHazardous(ReverseCommandNotHazardous);
			logicalDeviceRelayHBridgeStatusType.SetCommandForwardNotHazardous(ForwardCommandNotHazardous);
			logicalDeviceRelayHBridgeStatusType.SetPosition(Position);
			logicalDeviceRelayHBridgeStatusType.SetCurrentDrawAmps(CurrentDrawAmps);
			logicalDeviceRelayHBridgeStatusType.SetUserMessageDtc(UserMessageDtc);
			return logicalDeviceRelayHBridgeStatusType.CopyCurrentData();
		}

		public static LogicalDeviceRelayHBridgeStatusType2Serializable MakeStatusSerializable(byte[] rawData)
		{
			int num = rawData.Length;
			LogicalDeviceRelayHBridgeStatusType2 logicalDeviceRelayHBridgeStatusType = new LogicalDeviceRelayHBridgeStatusType2();
			if (num < logicalDeviceRelayHBridgeStatusType.MinSize || num > logicalDeviceRelayHBridgeStatusType.MaxSize)
			{
				throw new ArgumentOutOfRangeException("rawData", $"Data size must be between {logicalDeviceRelayHBridgeStatusType.MinSize} and {logicalDeviceRelayHBridgeStatusType.MaxSize}");
			}
			logicalDeviceRelayHBridgeStatusType.Update(rawData, rawData.Length);
			return new LogicalDeviceRelayHBridgeStatusType2Serializable(logicalDeviceRelayHBridgeStatusType);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceRelayHBridgeStatusType2Serializable");
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
			builder.Append(", UserClearRequired = ");
			builder.Append(UserClearRequired.ToString());
			builder.Append(", ReverseCommandNotHazardous = ");
			builder.Append(ReverseCommandNotHazardous.ToString());
			builder.Append(", ForwardCommandNotHazardous = ");
			builder.Append(ForwardCommandNotHazardous.ToString());
			builder.Append(", Position = ");
			builder.Append(Position.ToString());
			builder.Append(", CurrentDrawAmps = ");
			builder.Append(CurrentDrawAmps.ToString());
			builder.Append(", UserMessageDtc = ");
			builder.Append(UserMessageDtc.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceRelayHBridgeStatusType2Serializable? left, LogicalDeviceRelayHBridgeStatusType2Serializable? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceRelayHBridgeStatusType2Serializable? left, LogicalDeviceRelayHBridgeStatusType2Serializable? right)
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
			return ((((((base.GetHashCode() * -1521134295 + EqualityComparer<RelayHBridgeDirection>.Default.GetHashCode(State)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(UserClearRequired)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(ReverseCommandNotHazardous)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(ForwardCommandNotHazardous)) * -1521134295 + EqualityComparer<byte>.Default.GetHashCode(Position)) * -1521134295 + EqualityComparer<float>.Default.GetHashCode(CurrentDrawAmps)) * -1521134295 + EqualityComparer<DTC_ID>.Default.GetHashCode(UserMessageDtc);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceRelayHBridgeStatusType2Serializable);
		}

		[CompilerGenerated]
		public sealed override bool Equals(LogicalDeviceStatusSerializableBase<LogicalDeviceRelayHBridgeStatusType2Serializable>? other)
		{
			return Equals((object)other);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceRelayHBridgeStatusType2Serializable? other)
		{
			if ((object)this != other)
			{
				if (base.Equals(other) && EqualityComparer<RelayHBridgeDirection>.Default.Equals(State, other!.State) && EqualityComparer<bool>.Default.Equals(UserClearRequired, other!.UserClearRequired) && EqualityComparer<bool>.Default.Equals(ReverseCommandNotHazardous, other!.ReverseCommandNotHazardous) && EqualityComparer<bool>.Default.Equals(ForwardCommandNotHazardous, other!.ForwardCommandNotHazardous) && EqualityComparer<byte>.Default.Equals(Position, other!.Position) && EqualityComparer<float>.Default.Equals(CurrentDrawAmps, other!.CurrentDrawAmps))
				{
					return EqualityComparer<DTC_ID>.Default.Equals(UserMessageDtc, other!.UserMessageDtc);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceRelayHBridgeStatusType2Serializable(LogicalDeviceRelayHBridgeStatusType2Serializable original)
			: base((LogicalDeviceStatusSerializableBase<LogicalDeviceRelayHBridgeStatusType2Serializable>)original)
		{
			State = original.State;
			UserClearRequired = original.UserClearRequired;
			ReverseCommandNotHazardous = original.ReverseCommandNotHazardous;
			ForwardCommandNotHazardous = original.ForwardCommandNotHazardous;
			Position = original.Position;
			CurrentDrawAmps = original.CurrentDrawAmps;
			UserMessageDtc = original.UserMessageDtc;
		}
	}
}
