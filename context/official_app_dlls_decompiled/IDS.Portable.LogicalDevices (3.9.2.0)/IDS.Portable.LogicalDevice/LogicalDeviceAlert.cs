using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceAlert : ILogicalDeviceAlert, IJsonSerializerClass, IComparable<LogicalDeviceAlert>, IEquatable<LogicalDeviceAlert>
	{
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceAlert);
			}
		}

		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public string Name { get; }

		[JsonProperty]
		public bool IsActive { get; }

		[JsonProperty]
		public int? Count { get; }

		[JsonConstructor]
		public LogicalDeviceAlert(string name, bool isActive, int? count)
		{
			Name = name ?? string.Empty;
			IsActive = isActive;
			Count = count;
		}

		static LogicalDeviceAlert()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public virtual int CompareTo(LogicalDeviceAlert? other)
		{
			if ((object)this == other)
			{
				return 0;
			}
			if ((object)other == null)
			{
				return 1;
			}
			int num = string.Compare(Name, other!.Name, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			int num2 = IsActive.CompareTo(other!.IsActive);
			if (num2 != 0)
			{
				return num2;
			}
			return Nullable.Compare(Count, other!.Count);
		}

		public override string ToString()
		{
			return string.Format("Alert {0}[{1}/{2}]", Name, IsActive ? "ACTIVE" : "INACTIVE", Count);
		}

		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("SerializerClass = ");
			builder.Append((object)SerializerClass);
			builder.Append(", Name = ");
			builder.Append((object)Name);
			builder.Append(", IsActive = ");
			builder.Append(IsActive.ToString());
			builder.Append(", Count = ");
			builder.Append(Count.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceAlert? left, LogicalDeviceAlert? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceAlert? left, LogicalDeviceAlert? right)
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
			return ((EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name)) * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsActive)) * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Count);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceAlert);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceAlert? other)
		{
			if ((object)this != other)
			{
				if ((object)other != null && EqualityContract == other!.EqualityContract && EqualityComparer<string>.Default.Equals(Name, other!.Name) && EqualityComparer<bool>.Default.Equals(IsActive, other!.IsActive))
				{
					return EqualityComparer<int?>.Default.Equals(Count, other!.Count);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceAlert(LogicalDeviceAlert original)
		{
			Name = original.Name;
			IsActive = original.IsActive;
			Count = original.Count;
		}
	}
}
