using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Tag;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceFeatureManager<TLogicalDevice> : CommonDisposable where TLogicalDevice : ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures>, ILogicalDeviceFeature
	{
		public const string LogTag = "LogicalDeviceFeatureManager";

		private int _applyingPendingFeatures;

		public TLogicalDevice LogicalDevice { get; }

		public IEnumerable<(LogicalDeviceCapabilityFeatureId FeatureId, LogicalDeviceCapabilityFeatureStatus Status)> CapabilityFeatureStatus
		{
			[IteratorStateMachine(typeof(LogicalDeviceFeatureManager<>._003Cget_CapabilityFeatureStatus_003Ed__9))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				return new _003Cget_CapabilityFeatureStatus_003Ed__9(-2)
				{
					_003C_003E4__this = this
				};
			}
		}

		public LogicalDeviceFeatureManager(TLogicalDevice logicalDevice)
		{
			LogicalDevice = logicalDevice;
			TLogicalDevice logicalDevice2 = LogicalDevice;
			logicalDevice2.PropertyChanged += LogicalDeviceOnPropertyChanged;
			LogicalDevice.DeviceCapability.DeviceCapabilityChangedEvent += OnDeviceCapabilityWithFeatureChangedEvent;
		}

		public LogicalDeviceCapabilityFeatureStatus GetFeatureStatus(LogicalDeviceCapabilityFeatureId capabilityFeatureId)
		{
			if (capabilityFeatureId == LogicalDeviceCapabilityFeatureId.Unknown)
			{
				return LogicalDeviceCapabilityFeatureStatus.Unknown;
			}
			if (!Enumerable.Contains(LogicalDevice.DeviceCapability.CapabilityFeaturesAll, capabilityFeatureId))
			{
				return LogicalDeviceCapabilityFeatureStatus.NotSupported;
			}
			if (LogicalDevice.DeviceCapability.IsCapabilityFeatureEnabled(capabilityFeatureId))
			{
				return LogicalDeviceCapabilityFeatureStatus.Enabled;
			}
			if (GetFeatureStatusPending(capabilityFeatureId) != 0)
			{
				return LogicalDeviceCapabilityFeatureStatus.EnabledPending;
			}
			return LogicalDeviceCapabilityFeatureStatus.Disabled;
		}

		public LogicalDeviceCapabilityFeatureStatusPending GetFeatureStatusPending(LogicalDeviceCapabilityFeatureId capabilityFeatureId)
		{
			return LogicalDeviceTagCapabilityFeaturePending.GetTag(LogicalDevice, capabilityFeatureId)?.FeatureStatusPending ?? LogicalDeviceCapabilityFeatureStatusPending.None;
		}

		public async Task<(LogicalDeviceCapabilityFeatureStatus FeatureStatus, LogicalDeviceCapabilityFeatureStatusPending FeatureStatusPending)> TryEnableFeatureAsync(LogicalDeviceCapabilityFeatureId capabilityFeatureId, string requiredSoftwarePartNumber, CancellationToken cancellationToken)
		{
			if (!Enumerable.Contains(LogicalDevice.DeviceCapability.CapabilityFeaturesAll, capabilityFeatureId))
			{
				return (LogicalDeviceCapabilityFeatureStatus.NotSupported, LogicalDeviceCapabilityFeatureStatusPending.None);
			}
			if (GetFeatureStatus(capabilityFeatureId) == LogicalDeviceCapabilityFeatureStatus.Enabled)
			{
				return (LogicalDeviceCapabilityFeatureStatus.Enabled, LogicalDeviceCapabilityFeatureStatusPending.None);
			}
			LogicalDeviceTagCapabilityFeaturePending featurePendingTag = LogicalDeviceTagCapabilityFeaturePending.GetTag(LogicalDevice, capabilityFeatureId);
			LogicalDeviceCapabilityFeatureStatusPending pendingStatus = LogicalDeviceCapabilityFeatureStatusPending.PendingOther;
			try
			{
				pendingStatus = await LogicalDevice.DeviceCapability.SetCapabilityFeatureAsync(LogicalDevice, capabilityFeatureId, requiredSoftwarePartNumber, cancellationToken);
				switch (pendingStatus)
				{
				case LogicalDeviceCapabilityFeatureStatusPending.None:
					if (featurePendingTag == null)
					{
						AlertFeatureEnabled(capabilityFeatureId);
					}
					return (LogicalDeviceCapabilityFeatureStatus.Enabled, LogicalDeviceCapabilityFeatureStatusPending.None);
				default:
					TaggedLog.Warning("LogicalDeviceFeatureManager", $"Unknown Pending State {pendingStatus}");
					pendingStatus = LogicalDeviceCapabilityFeatureStatusPending.PendingOther;
					break;
				case LogicalDeviceCapabilityFeatureStatusPending.PendingFirmwareUpdate:
				case LogicalDeviceCapabilityFeatureStatusPending.PendingOnline:
				case LogicalDeviceCapabilityFeatureStatusPending.PendingOther:
					break;
				}
			}
			catch (LogicalDeviceCapabilityFeatureNotSupported logicalDeviceCapabilityFeatureNotSupported)
			{
				TaggedLog.Warning("LogicalDeviceFeatureManager", $"Feature {capabilityFeatureId} currently not supported and is being marked as {pendingStatus} for {LogicalDevice}: {logicalDeviceCapabilityFeatureNotSupported.Message}");
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("LogicalDeviceFeatureManager", $"Feature {capabilityFeatureId} not enabled and is being marked as {pendingStatus} for {LogicalDevice}: {ex.Message}");
			}
			LogicalDeviceTagCapabilityFeaturePending logicalDeviceTagCapabilityFeaturePending = featurePendingTag;
			if ((object)logicalDeviceTagCapabilityFeaturePending == null || logicalDeviceTagCapabilityFeaturePending.FeatureStatusPending != pendingStatus)
			{
				if ((object)featurePendingTag == null)
				{
					LogicalDeviceTagCapabilityFeaturePending.SetTag(tag: new LogicalDeviceTagCapabilityFeaturePending(capabilityFeatureId, requiredSoftwarePartNumber, pendingStatus), logicalDevice: LogicalDevice);
				}
				else
				{
					featurePendingTag.FeatureStatusPending = pendingStatus;
				}
			}
			return (LogicalDeviceCapabilityFeatureStatus.EnabledPending, pendingStatus);
		}

		private void OnDeviceCapabilityWithFeatureChangedEvent()
		{
			foreach (LogicalDeviceTagCapabilityFeaturePending item in Enumerable.ToList(LogicalDeviceTagCapabilityFeaturePending.GetTags(LogicalDevice)))
			{
				if (GetFeatureStatus(item.CapabilityFeatureId) == LogicalDeviceCapabilityFeatureStatus.Enabled)
				{
					LogicalDeviceTagCapabilityFeaturePending.RemoveTag(LogicalDevice, item);
					AlertFeatureEnabled(item.CapabilityFeatureId);
				}
			}
		}

		private void AlertFeatureEnabled(LogicalDeviceCapabilityFeatureId capabilityFeatureId)
		{
			TaggedLog.Information("LogicalDeviceFeatureManager", string.Format("{0}: {1} is now Enabled for {2}", "TryEnableFeatureAsync", capabilityFeatureId, LogicalDevice));
			LogicalDevice.UpdateAlert(capabilityFeatureId, LogicalDeviceCapabilityFeatureStatus.Enabled, LogicalDeviceCapabilityFeatureStatusPending.None);
			MainThread.RequestMainThreadAction(delegate
			{
				LogicalDevice.DeviceService.DeviceManager!.ContainerDataSourceSync(batchRequest: true);
			});
		}

		private async Task TryApplyPendingFeaturesAsync(CancellationToken cancellationToken)
		{
			if (Interlocked.Exchange(ref _applyingPendingFeatures, 1) == 1)
			{
				return;
			}
			try
			{
				foreach (LogicalDeviceTagCapabilityFeaturePending pendingFeatureTag in Enumerable.ToList(LogicalDeviceTagCapabilityFeaturePending.GetTags(LogicalDevice)))
				{
					(LogicalDeviceCapabilityFeatureStatus, LogicalDeviceCapabilityFeatureStatusPending) tuple = await TryEnableFeatureAsync(pendingFeatureTag.CapabilityFeatureId, pendingFeatureTag.SoftwarePartNumber, cancellationToken);
					TaggedLog.Information("LogicalDeviceFeatureManager", $"Apply Pending Feature {pendingFeatureTag.CapabilityFeatureId} {tuple.Item1}/{tuple.Item2}");
				}
			}
			finally
			{
				_applyingPendingFeatures = 0;
			}
		}

		private void LogicalDeviceOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ActiveConnection")
			{
				TryApplyPendingFeaturesAsync(CancellationToken.None);
			}
		}

		public override void Dispose(bool disposing)
		{
			TLogicalDevice logicalDevice = LogicalDevice;
			logicalDevice.PropertyChanged -= LogicalDeviceOnPropertyChanged;
		}
	}
}
