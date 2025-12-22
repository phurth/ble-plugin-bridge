using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceFeature : ICommonDisposable, IDisposable
	{
		IEnumerable<(LogicalDeviceCapabilityFeatureId FeatureId, LogicalDeviceCapabilityFeatureStatus Status)> CapabilityFeatureStatus { get; }

		Task<(LogicalDeviceCapabilityFeatureStatus FeatureStatus, LogicalDeviceCapabilityFeatureStatusPending FeatureStatusPending)> TryEnableFeatureAsync(LogicalDeviceCapabilityFeatureId capabilityFeatureId, string requiredSoftwarePartNumber, CancellationToken cancellationToken);

		LogicalDeviceCapabilityFeatureStatus GetFeatureStatus(LogicalDeviceCapabilityFeatureId capabilityFeatureId);

		LogicalDeviceCapabilityFeatureStatusPending GetFeatureStatusPending(LogicalDeviceCapabilityFeatureId capabilityFeatureId);

		void UpdateAlert(LogicalDeviceCapabilityFeatureId capabilityFeatureId, LogicalDeviceCapabilityFeatureStatus featureStatus, LogicalDeviceCapabilityFeatureStatusPending featureStatusPending);
	}
}
