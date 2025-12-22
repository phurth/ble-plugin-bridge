using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using Newtonsoft.Json;
using OneControl.Devices.TPMS;

namespace IDS.Portable.LogicalDevice.Tag
{
	[JsonObject(MemberSerialization.OptIn)]
	internal class LogicalDeviceTagTpms : ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass, IEquatable<LogicalDeviceTagTpms>
	{
		[JsonIgnore]
		private const TpmsGroupId DefaultGroupId = TpmsGroupId.GroupId0;

		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceTagTpms);
			}
		}

		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public TpmsGroupId ActiveTrailer { get; set; }

		[JsonConstructor]
		public LogicalDeviceTagTpms(TpmsGroupId? selectedTrailer)
		{
			ActiveTrailer = selectedTrailer.GetValueOrDefault();
		}

		public virtual bool Equals(ILogicalDeviceTag other)
		{
			return Equals((object)other);
		}

		static LogicalDeviceTagTpms()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return $"ActiveTrailer: {ActiveTrailer}";
		}

		public static LogicalDeviceTagTpms? GetTag(ILogicalDeviceTpms logicalDevice)
		{
			return Enumerable.FirstOrDefault(logicalDevice.DeviceService.DeviceManager!.TagManager.GetTags<LogicalDeviceTagTpms>(logicalDevice));
		}

		public static void RemoveTag(ILogicalDeviceTpms logicalDevice, LogicalDeviceTagTpms tag)
		{
			logicalDevice.DeviceService.DeviceManager!.TagManager.RemoveTag(tag, logicalDevice);
		}

		public static void SetTag(ILogicalDeviceTpms logicalDevice, LogicalDeviceTagTpms tag)
		{
			LogicalDeviceTagManager tagManager = logicalDevice.DeviceService.DeviceManager!.TagManager;
			LogicalDeviceTagTpms tag2 = GetTag(logicalDevice);
			if ((object)tag != tag2)
			{
				if ((object)tag2 != null)
				{
					tagManager.RemoveTag(tag2, logicalDevice);
				}
				tagManager.AddTag(tag, logicalDevice);
			}
		}

		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("SerializerClass = ");
			builder.Append((object)SerializerClass);
			builder.Append(", ActiveTrailer = ");
			builder.Append(ActiveTrailer.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceTagTpms? left, LogicalDeviceTagTpms? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceTagTpms? left, LogicalDeviceTagTpms? right)
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
			return EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<TpmsGroupId>.Default.GetHashCode(ActiveTrailer);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceTagTpms);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceTagTpms? other)
		{
			if ((object)this != other)
			{
				if ((object)other != null && EqualityContract == other!.EqualityContract)
				{
					return EqualityComparer<TpmsGroupId>.Default.Equals(ActiveTrailer, other!.ActiveTrailer);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceTagTpms(LogicalDeviceTagTpms original)
		{
			ActiveTrailer = original.ActiveTrailer;
		}
	}
}
