using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice.Tag
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagCapabilityFeaturePending : ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass, IEquatable<LogicalDeviceTagCapabilityFeaturePending>
	{
		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceTagCapabilityFeaturePending);
			}
		}

		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public LogicalDeviceCapabilityFeatureId CapabilityFeatureId { get; }

		[JsonProperty]
		public string SoftwarePartNumber { get; }

		[JsonProperty]
		public LogicalDeviceCapabilityFeatureStatusPending FeatureStatusPending { get; set; }

		[JsonConstructor]
		public LogicalDeviceTagCapabilityFeaturePending(LogicalDeviceCapabilityFeatureId capabilityFeatureId, string softwarePartNumber, LogicalDeviceCapabilityFeatureStatusPending featureStatusPending)
		{
			CapabilityFeatureId = capabilityFeatureId;
			SoftwarePartNumber = softwarePartNumber;
			FeatureStatusPending = featureStatusPending;
		}

		public virtual bool Equals(ILogicalDeviceTag other)
		{
			return Equals((object)other);
		}

		static LogicalDeviceTagCapabilityFeaturePending()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return $"{CapabilityFeatureId} for {SoftwarePartNumber}";
		}

		public static LogicalDeviceTagCapabilityFeaturePending? GetTag(ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures> logicalDevice, LogicalDeviceCapabilityFeatureId capabilityFeatureId)
		{
			return Enumerable.FirstOrDefault(logicalDevice.DeviceService.DeviceManager!.TagManager.GetTags<LogicalDeviceTagCapabilityFeaturePending>(logicalDevice), (LogicalDeviceTagCapabilityFeaturePending tag) => tag.CapabilityFeatureId == capabilityFeatureId);
		}

		public static IEnumerable<LogicalDeviceTagCapabilityFeaturePending> GetTags(ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures> logicalDevice)
		{
			return logicalDevice.DeviceService.DeviceManager!.TagManager.GetTags<LogicalDeviceTagCapabilityFeaturePending>(logicalDevice);
		}

		public static void RemoveTag(ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures> logicalDevice, LogicalDeviceTagCapabilityFeaturePending tag)
		{
			logicalDevice.DeviceService.DeviceManager!.TagManager.RemoveTag(tag, logicalDevice);
		}

		public static void SetTag(ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures> logicalDevice, LogicalDeviceTagCapabilityFeaturePending tag)
		{
			LogicalDeviceTagManager tagManager = logicalDevice.DeviceService.DeviceManager!.TagManager;
			LogicalDeviceTagCapabilityFeaturePending tag2 = GetTag(logicalDevice, tag.CapabilityFeatureId);
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
			builder.Append(", CapabilityFeatureId = ");
			builder.Append(CapabilityFeatureId.ToString());
			builder.Append(", SoftwarePartNumber = ");
			builder.Append((object)SoftwarePartNumber);
			builder.Append(", FeatureStatusPending = ");
			builder.Append(FeatureStatusPending.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceTagCapabilityFeaturePending? left, LogicalDeviceTagCapabilityFeaturePending? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceTagCapabilityFeaturePending? left, LogicalDeviceTagCapabilityFeaturePending? right)
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
			return ((EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<LogicalDeviceCapabilityFeatureId>.Default.GetHashCode(CapabilityFeatureId)) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SoftwarePartNumber)) * -1521134295 + EqualityComparer<LogicalDeviceCapabilityFeatureStatusPending>.Default.GetHashCode(FeatureStatusPending);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceTagCapabilityFeaturePending);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceTagCapabilityFeaturePending? other)
		{
			if ((object)this != other)
			{
				if ((object)other != null && EqualityContract == other!.EqualityContract && EqualityComparer<LogicalDeviceCapabilityFeatureId>.Default.Equals(CapabilityFeatureId, other!.CapabilityFeatureId) && EqualityComparer<string>.Default.Equals(SoftwarePartNumber, other!.SoftwarePartNumber))
				{
					return EqualityComparer<LogicalDeviceCapabilityFeatureStatusPending>.Default.Equals(FeatureStatusPending, other!.FeatureStatusPending);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceTagCapabilityFeaturePending(LogicalDeviceTagCapabilityFeaturePending original)
		{
			CapabilityFeatureId = original.CapabilityFeatureId;
			SoftwarePartNumber = original.SoftwarePartNumber;
			FeatureStatusPending = original.FeatureStatusPending;
		}
	}
}
