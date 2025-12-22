using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Devices
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceRelayBasicStatusType1Serializable : LogicalDeviceStatusSerializableBase<LogicalDeviceRelayBasicStatusType1Serializable>, ILogicalDeviceRelayBasicStatusSerializable, ILogicalDeviceStatusSerializable, IJsonSerializerClass, IEquatable<LogicalDeviceRelayBasicStatusType1Serializable>
	{
		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceRelayBasicStatusType1Serializable);
			}
		}

		[JsonProperty]
		public bool IsFaulted { get; }

		[JsonProperty]
		public bool IsOn { get; }

		[JsonProperty]
		public bool UserClearRequired { get; }

		[JsonConstructor]
		public LogicalDeviceRelayBasicStatusType1Serializable(bool isFaulted, bool isOn, bool userClearRequired)
		{
			IsFaulted = isFaulted;
			IsOn = isOn;
			UserClearRequired = userClearRequired;
		}

		public LogicalDeviceRelayBasicStatusType1Serializable(ILogicalDeviceRelayBasicStatus status)
			: this(status.IsFaulted, status.IsOn, status.UserClearRequired)
		{
		}

		public override byte[] MakeRawData()
		{
			LogicalDeviceRelayBasicStatusType1 logicalDeviceRelayBasicStatusType = new LogicalDeviceRelayBasicStatusType1();
			logicalDeviceRelayBasicStatusType.SetState(IsOn);
			logicalDeviceRelayBasicStatusType.SetFault(IsFaulted);
			logicalDeviceRelayBasicStatusType.SetUserClearRequired(UserClearRequired);
			return logicalDeviceRelayBasicStatusType.CopyCurrentData();
		}

		public static LogicalDeviceRelayBasicStatusType1Serializable MakeStatusSerializable(byte[] rawData)
		{
			int num = rawData.Length;
			LogicalDeviceRelayBasicStatusType1 logicalDeviceRelayBasicStatusType = new LogicalDeviceRelayBasicStatusType1();
			if (num < logicalDeviceRelayBasicStatusType.MinSize || num > logicalDeviceRelayBasicStatusType.MaxSize)
			{
				throw new ArgumentOutOfRangeException("rawData", $"Data size must be between {logicalDeviceRelayBasicStatusType.MinSize} and {logicalDeviceRelayBasicStatusType.MaxSize}");
			}
			logicalDeviceRelayBasicStatusType.Update(rawData, rawData.Length);
			return new LogicalDeviceRelayBasicStatusType1Serializable(logicalDeviceRelayBasicStatusType);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceRelayBasicStatusType1Serializable");
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
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceRelayBasicStatusType1Serializable? left, LogicalDeviceRelayBasicStatusType1Serializable? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceRelayBasicStatusType1Serializable? left, LogicalDeviceRelayBasicStatusType1Serializable? right)
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
			return ((base.GetHashCode() * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsFaulted)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsOn)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(UserClearRequired);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceRelayBasicStatusType1Serializable);
		}

		[CompilerGenerated]
		public sealed override bool Equals(LogicalDeviceStatusSerializableBase<LogicalDeviceRelayBasicStatusType1Serializable>? other)
		{
			return Equals((object)other);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceRelayBasicStatusType1Serializable? other)
		{
			if ((object)this != other)
			{
				if (base.Equals(other) && EqualityComparer<bool>.Default.Equals(IsFaulted, other!.IsFaulted) && EqualityComparer<bool>.Default.Equals(IsOn, other!.IsOn))
				{
					return EqualityComparer<bool>.Default.Equals(UserClearRequired, other!.UserClearRequired);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceRelayBasicStatusType1Serializable(LogicalDeviceRelayBasicStatusType1Serializable original)
			: base((LogicalDeviceStatusSerializableBase<LogicalDeviceRelayBasicStatusType1Serializable>)original)
		{
			IsFaulted = original.IsFaulted;
			IsOn = original.IsOn;
			UserClearRequired = original.UserClearRequired;
		}
	}
}
