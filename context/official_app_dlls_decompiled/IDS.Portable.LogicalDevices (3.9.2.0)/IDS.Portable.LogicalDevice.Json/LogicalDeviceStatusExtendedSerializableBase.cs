using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice.Json
{
	[JsonObject(MemberSerialization.OptIn)]
	public abstract class LogicalDeviceStatusExtendedSerializableBase<TSerializable> : ILogicalDeviceStatusExtendedSerializable, IJsonSerializerClass, IEquatable<LogicalDeviceStatusExtendedSerializableBase<TSerializable>> where TSerializable : class
	{
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceStatusExtendedSerializableBase<TSerializable>);
			}
		}

		[JsonProperty]
		public string SerializerClass => GetType().Name;

		[JsonConstructor]
		protected LogicalDeviceStatusExtendedSerializableBase()
		{
		}

		public abstract IReadOnlyDictionary<byte, byte[]> MakeRawDataExtended();

		protected static void RegisterJsonSerializer()
		{
			Type typeFromHandle = typeof(TSerializable);
			TypeRegistry.Register(typeFromHandle.Name, typeFromHandle);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceStatusExtendedSerializableBase");
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
			builder.Append("SerializerClass = ");
			builder.Append((object)SerializerClass);
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceStatusExtendedSerializableBase<TSerializable>? left, LogicalDeviceStatusExtendedSerializableBase<TSerializable>? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceStatusExtendedSerializableBase<TSerializable>? left, LogicalDeviceStatusExtendedSerializableBase<TSerializable>? right)
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
			return EqualityComparer<Type>.Default.GetHashCode(EqualityContract);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceStatusExtendedSerializableBase<TSerializable>);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceStatusExtendedSerializableBase<TSerializable>? other)
		{
			if ((object)this != other)
			{
				if ((object)other != null)
				{
					return EqualityContract == other!.EqualityContract;
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceStatusExtendedSerializableBase(LogicalDeviceStatusExtendedSerializableBase<TSerializable> original)
		{
		}
	}
}
