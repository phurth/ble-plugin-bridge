using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceAlertCapabilityFeature : LogicalDeviceAlert, IEquatable<LogicalDeviceAlertCapabilityFeature>
	{
		public const string AlertNamePrefix = "AlertCapabilityFeature";

		[CompilerGenerated]
		protected override Type EqualityContract
		{
			[CompilerGenerated]
			get
			{
				return typeof(LogicalDeviceAlertCapabilityFeature);
			}
		}

		[JsonProperty]
		public LogicalDeviceCapabilityFeatureId CapabilityFeatureId { get; }

		[JsonProperty]
		private LogicalDeviceCapabilityFeatureStatus FeatureStatus { get; }

		[JsonProperty]
		private LogicalDeviceCapabilityFeatureStatusPending FeatureStatusPending { get; }

		public static string MakeAlertName(LogicalDeviceCapabilityFeatureId capabilityFeatureId)
		{
			return "AlertCapabilityFeature" + capabilityFeatureId;
		}

		public LogicalDeviceAlertCapabilityFeature(LogicalDeviceCapabilityFeatureId capabilityFeatureId, LogicalDeviceCapabilityFeatureStatus featureStatus, LogicalDeviceCapabilityFeatureStatusPending featureStatusPending)
			: base(MakeAlertName(capabilityFeatureId), isActive: true, null)
		{
			CapabilityFeatureId = capabilityFeatureId;
			FeatureStatus = featureStatus;
			FeatureStatusPending = featureStatusPending;
		}

		public override int CompareTo(LogicalDeviceAlert? other)
		{
			int num = base.CompareTo(other);
			if (num != 0)
			{
				return num;
			}
			if (!(other is LogicalDeviceAlertCapabilityFeature logicalDeviceAlertCapabilityFeature))
			{
				return num;
			}
			int num2 = FeatureStatus.CompareTo(logicalDeviceAlertCapabilityFeature.FeatureStatus);
			if (num2 != 0)
			{
				return num2;
			}
			return FeatureStatusPending.CompareTo(logicalDeviceAlertCapabilityFeature.FeatureStatusPending);
		}

		public override string ToString()
		{
			return string.Format("Alert {0}[{1}/{2}]", CapabilityFeatureId, base.IsActive ? "ACTIVE" : "INACTIVE", base.Count);
		}

		[CompilerGenerated]
		protected override bool PrintMembers(StringBuilder builder)
		{
			if (base.PrintMembers(builder))
			{
				builder.Append(", ");
			}
			builder.Append("CapabilityFeatureId = ");
			builder.Append(CapabilityFeatureId.ToString());
			return true;
		}

		[CompilerGenerated]
		public static bool operator !=(LogicalDeviceAlertCapabilityFeature? left, LogicalDeviceAlertCapabilityFeature? right)
		{
			return !(left == right);
		}

		[CompilerGenerated]
		public static bool operator ==(LogicalDeviceAlertCapabilityFeature? left, LogicalDeviceAlertCapabilityFeature? right)
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
			return ((base.GetHashCode() * -1521134295 + EqualityComparer<LogicalDeviceCapabilityFeatureId>.Default.GetHashCode(CapabilityFeatureId)) * -1521134295 + EqualityComparer<LogicalDeviceCapabilityFeatureStatus>.Default.GetHashCode(FeatureStatus)) * -1521134295 + EqualityComparer<LogicalDeviceCapabilityFeatureStatusPending>.Default.GetHashCode(FeatureStatusPending);
		}

		[CompilerGenerated]
		public override bool Equals(object? obj)
		{
			return Equals(obj as LogicalDeviceAlertCapabilityFeature);
		}

		[CompilerGenerated]
		public sealed override bool Equals(LogicalDeviceAlert? other)
		{
			return Equals((object)other);
		}

		[CompilerGenerated]
		public virtual bool Equals(LogicalDeviceAlertCapabilityFeature? other)
		{
			if ((object)this != other)
			{
				if (base.Equals(other) && EqualityComparer<LogicalDeviceCapabilityFeatureId>.Default.Equals(CapabilityFeatureId, other!.CapabilityFeatureId) && EqualityComparer<LogicalDeviceCapabilityFeatureStatus>.Default.Equals(FeatureStatus, other!.FeatureStatus))
				{
					return EqualityComparer<LogicalDeviceCapabilityFeatureStatusPending>.Default.Equals(FeatureStatusPending, other!.FeatureStatusPending);
				}
				return false;
			}
			return true;
		}

		[CompilerGenerated]
		protected LogicalDeviceAlertCapabilityFeature(LogicalDeviceAlertCapabilityFeature original)
			: base(original)
		{
			CapabilityFeatureId = original.CapabilityFeatureId;
			FeatureStatus = original.FeatureStatus;
			FeatureStatusPending = original.FeatureStatusPending;
		}
	}
}
