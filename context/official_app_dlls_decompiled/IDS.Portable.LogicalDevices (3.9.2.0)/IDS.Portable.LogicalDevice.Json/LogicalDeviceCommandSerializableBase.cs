using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice.Json
{
	[JsonObject(MemberSerialization.OptIn)]
	public abstract class LogicalDeviceCommandSerializableBase<TSerializable, TBaseCommand> : ILogicalDeviceCommandSerializable, IJsonSerializerClass, IEquatable<LogicalDeviceCommandSerializableBase<TSerializable, TBaseCommand>> where TSerializable : class where TBaseCommand : LogicalDeviceCommandPacket
	{
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceCommandSerializableBase<TSerializable, TBaseCommand>);
			}
		}

		[JsonProperty]
		public string SerializerClass => GetType().Name;

		[JsonConstructor]
		protected LogicalDeviceCommandSerializableBase()
		{
		}

		public abstract TBaseCommand ToLogicalDeviceCommand();

		protected static void RegisterJsonSerializer()
		{
			Type typeFromHandle = typeof(TSerializable);
			TypeRegistry.Register(typeFromHandle.Name, typeFromHandle);
		}

		[CompilerGenerated]
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("LogicalDeviceCommandSerializableBase");
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
		public static bool operator !=(LogicalDeviceCommandSerializableBase<TSerializable, TBaseCommand>? left, LogicalDeviceCommandSerializableBase<TSerializable, TBaseCommand>? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceCommandSerializableBase<TSerializable, TBaseCommand>? left, LogicalDeviceCommandSerializableBase<TSerializable, TBaseCommand>? right)
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
			return Equals(obj as LogicalDeviceCommandSerializableBase<TSerializable, TBaseCommand>);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceCommandSerializableBase<TSerializable, TBaseCommand>? other)
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
		protected LogicalDeviceCommandSerializableBase(LogicalDeviceCommandSerializableBase<TSerializable, TBaseCommand> original)
		{
		}
	}
}
