using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using Newtonsoft.Json;
using OneControl.Devices.Leveler.Type5;

namespace IDS.Portable.LogicalDevice.Tag
{
	[JsonObject(MemberSerialization.OptIn)]
	internal class LogicalDeviceTagLeveler5SetPoints : ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass, IEquatable<LogicalDeviceTagLeveler5SetPoints>
	{
		[JsonIgnore]
		private LevelerAutoOperationSetPoint DefaultSelectedSetPoint;

		[CompilerGenerated]
		protected virtual Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceTagLeveler5SetPoints);
			}
		}

		[JsonProperty]
		public virtual string SerializerClass => GetType().Name;

		[JsonProperty]
		public LevelerAutoOperationSetPoint SelectedSetPoint { get; set; }

		[JsonProperty]
		public ConcurrentDictionary<LevelerAutoOperationSetPoint, string>? CustomSetPointNames { get; set; }

		[JsonConstructor]
		public LogicalDeviceTagLeveler5SetPoints(LevelerAutoOperationSetPoint? selectedSetPoint, ConcurrentDictionary<LevelerAutoOperationSetPoint, string>? customSetPointNames)
		{
			SelectedSetPoint = ((!selectedSetPoint.HasValue) ? DefaultSelectedSetPoint : selectedSetPoint.Value);
			CustomSetPointNames = customSetPointNames;
		}

		public virtual bool Equals(ILogicalDeviceTag other)
		{
			return Equals((object)other);
		}

		static LogicalDeviceTagLeveler5SetPoints()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return $"SelectedSetPoint: {SelectedSetPoint}, CustomSetPointNames {CustomSetPointNames}";
		}

		public static LogicalDeviceTagLeveler5SetPoints? GetTag(ILogicalDeviceLevelerType5 logicalDevice)
		{
			return Enumerable.FirstOrDefault(logicalDevice.DeviceService.DeviceManager!.TagManager.GetTags<LogicalDeviceTagLeveler5SetPoints>(logicalDevice));
		}

		public static void RemoveTag(ILogicalDeviceLevelerType5 logicalDevice, LogicalDeviceTagLeveler5SetPoints tag)
		{
			logicalDevice.DeviceService.DeviceManager!.TagManager.RemoveTag(tag, logicalDevice);
		}

		public static void SetTag(ILogicalDeviceLevelerType5 logicalDevice, LogicalDeviceTagLeveler5SetPoints tag)
		{
			LogicalDeviceTagManager tagManager = logicalDevice.DeviceService.DeviceManager!.TagManager;
			LogicalDeviceTagLeveler5SetPoints tag2 = GetTag(logicalDevice);
			if ((object)tag != tag2)
			{
				if ((object)tag2 != null)
				{
					tagManager.RemoveTag(tag2, logicalDevice);
				}
				tagManager.AddTag(tag, logicalDevice);
			}
		}

		public bool IsCustomTagAvailable(LogicalDeviceTagLeveler5SetPoints tag, LevelerAutoOperationSetPoint setPoint)
		{
			if (tag.CustomSetPointNames == null || !tag.CustomSetPointNames!.ContainsKey(setPoint))
			{
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected virtual bool PrintMembers(StringBuilder builder)
		{
			RuntimeHelpers.EnsureSufficientExecutionStack();
			builder.Append("SerializerClass = ");
			builder.Append((object)SerializerClass);
			builder.Append(", SelectedSetPoint = ");
			builder.Append(SelectedSetPoint.ToString());
			builder.Append(", CustomSetPointNames = ");
			builder.Append(CustomSetPointNames);
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceTagLeveler5SetPoints? left, LogicalDeviceTagLeveler5SetPoints? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceTagLeveler5SetPoints? left, LogicalDeviceTagLeveler5SetPoints? right)
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
			return ((EqualityComparer<Type>.Default.GetHashCode(EqualityContract) * -1521134295 + EqualityComparer<LevelerAutoOperationSetPoint>.Default.GetHashCode(DefaultSelectedSetPoint)) * -1521134295 + EqualityComparer<LevelerAutoOperationSetPoint>.Default.GetHashCode(SelectedSetPoint)) * -1521134295 + EqualityComparer<ConcurrentDictionary<LevelerAutoOperationSetPoint, string>>.Default.GetHashCode(CustomSetPointNames);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceTagLeveler5SetPoints);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceTagLeveler5SetPoints? other)
		{
			if ((object)this != other)
			{
				if ((object)other != null && EqualityContract == other!.EqualityContract && EqualityComparer<LevelerAutoOperationSetPoint>.Default.Equals(DefaultSelectedSetPoint, other!.DefaultSelectedSetPoint) && EqualityComparer<LevelerAutoOperationSetPoint>.Default.Equals(SelectedSetPoint, other!.SelectedSetPoint))
				{
					return EqualityComparer<ConcurrentDictionary<LevelerAutoOperationSetPoint, string>>.Default.Equals(CustomSetPointNames, other!.CustomSetPointNames);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceTagLeveler5SetPoints(LogicalDeviceTagLeveler5SetPoints original)
		{
			DefaultSelectedSetPoint = original.DefaultSelectedSetPoint;
			SelectedSetPoint = original.SelectedSetPoint;
			CustomSetPointNames = original.CustomSetPointNames;
		}
	}
}
