using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceCapabilitySerializable : IEquatable<LogicalDeviceCapabilitySerializable>
	{
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceCapabilitySerializable);
			}
		}

		[JsonProperty]
		public string CapabilityName { get; }

		public LogicalDeviceCapabilitySerializable(string capabilityName)
		{
			CapabilityName = capabilityName;
		}

		public static implicit operator LogicalDeviceCapabilitySerializable(string capabilityName)
		{
			return new LogicalDeviceCapabilitySerializable(capabilityName);
		}

		public static implicit operator string(LogicalDeviceCapabilitySerializable capabilitySerializable)
		{
			return capabilitySerializable.CapabilityName;
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceCapabilitySerializable");
			stringBuilder.Append(" { ");
			if (PrintMembers(stringBuilder))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("CapabilityName = ");
			builder.Append((object)CapabilityName);
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceCapabilitySerializable? left, LogicalDeviceCapabilitySerializable? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceCapabilitySerializable? left, LogicalDeviceCapabilitySerializable? right)
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
			return EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CapabilityName);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceCapabilitySerializable);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceCapabilitySerializable? other)
		{
			if ((object)this != other)
			{
				if ((object)other != null && EqualityContract == other!.EqualityContract)
				{
					return EqualityComparer<string>.Default.Equals(CapabilityName, other!.CapabilityName);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceCapabilitySerializable(LogicalDeviceCapabilitySerializable original)
		{
			CapabilityName = original.CapabilityName;
		}
	}
}
