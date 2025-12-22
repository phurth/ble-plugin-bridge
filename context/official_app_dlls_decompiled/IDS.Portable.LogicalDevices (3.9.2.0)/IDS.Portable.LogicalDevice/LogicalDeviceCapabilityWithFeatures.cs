using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public abstract class LogicalDeviceCapabilityWithFeatures : LogicalDeviceCapability, ILogicalDeviceCapabilityWithFeatures, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		public const string LogTag = "LogicalDeviceCapabilityWithFeatures";

		private const int ApplyPendingUpdateMaxAttempt = 3;

		private const int ApplyPendingUpdateAttemptDelayMs = 250;

		public PID CapabilitiesSupportedByFirmwarePid = PID.OPTIONAL_CAPABILITIES_SUPPORTED;

		public PID CapabilitiesEnabledPid = PID.OPTIONAL_CAPABILITIES_ENABLED;

		public PID CapabilitiesMandatoryByFirmwarePid = PID.OPTIONAL_CAPABILITIES_MANDATORY;

		public PID CapabilitiesUserDisabledPid = PID.OPTIONAL_CAPABILITIES_USER_DISABLED;

		public CapabilityWithFeaturesOption Options { get; }

		protected LogicalDevicePidSimByte? CapabilitiesSupportedPidSim { get; set; }

		protected LogicalDevicePidSimByte? CapabilitiesEnabledPidSim { get; set; }

		protected LogicalDevicePidSimByte? CapabilitiesMandatoryPidSim { get; set; }

		protected LogicalDevicePidSimByte? CapabilitiesUserDisabledPidSim { get; set; }

		public abstract IReadOnlyList<LogicalDeviceCapabilityFeatureId> CapabilityFeaturesAll { get; }

		protected LogicalDeviceCapabilityWithFeatures(byte? rawCapability, CapabilityWithFeaturesOption options)
			: base(rawCapability)
		{
			Options = options;
		}

		protected abstract byte GetCapabilityFeatureBitMask(LogicalDeviceCapabilityFeatureId capabilityFeatureId);

		public virtual bool IsCapabilityFeatureEnabled(LogicalDeviceCapabilityFeatureId capabilityFeatureId)
		{
			if (!Enumerable.Contains(CapabilityFeaturesAll, capabilityFeatureId))
			{
				return false;
			}
			byte capabilityFeatureBitMask = GetCapabilityFeatureBitMask(capabilityFeatureId);
			if (capabilityFeatureBitMask == 0)
			{
				return false;
			}
			return (RawValue & capabilityFeatureBitMask) != 0;
		}

		private IEnumerable<LogicalDeviceCapabilityFeatureId> FeatureIdFromRawData(byte supportedFeaturesRaw)
		{
			HashSet<LogicalDeviceCapabilityFeatureId> hashSet = new HashSet<LogicalDeviceCapabilityFeatureId>();
			foreach (LogicalDeviceCapabilityFeatureId item in CapabilityFeaturesAll)
			{
				byte capabilityFeatureBitMask = GetCapabilityFeatureBitMask(item);
				if ((supportedFeaturesRaw & capabilityFeatureBitMask) != 0)
				{
					hashSet.Add(item);
				}
			}
			return hashSet;
		}

		public async Task<IEnumerable<LogicalDeviceCapabilityFeatureId>> GetCapabilitiesUserDisabledAsync(ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures> logicalDevice, CancellationToken cancellationToken)
		{
			if (!Options.HasFlag(CapabilityWithFeaturesOption.SupportsUserDisabledFeatures))
			{
				throw new LogicalDeviceCapabilityFeatureNotSupported(string.Format("{0} not support for this device {1} using {2}", "GetCapabilitiesUserDisabledAsync", logicalDevice, GetType().Name));
			}
			ILogicalDevicePidByte capabilitiesUserDisabledPidSim = CapabilitiesUserDisabledPidSim;
			return FeatureIdFromRawData(await (capabilitiesUserDisabledPidSim ?? new LogicalDevicePidByte(logicalDevice, CapabilitiesUserDisabledPid, LogicalDeviceSessionType.Diagnostic)).ReadByteAsync(cancellationToken));
		}

		public async Task SetCapabilitiesUserDisabledAsync(ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures> logicalDevice, LogicalDeviceCapabilityFeatureId capabilityFeatureId, bool disable, CancellationToken cancellationToken)
		{
			if (!Options.HasFlag(CapabilityWithFeaturesOption.SupportsUserDisabledFeatures))
			{
				throw new LogicalDeviceCapabilityFeatureNotSupported(string.Format("{0} not support for this device {1} using {2}", "SetCapabilitiesUserDisabledAsync", logicalDevice, GetType().Name));
			}
			byte featureRawBitmask = GetCapabilityFeatureBitMask(capabilityFeatureId);
			if (featureRawBitmask == 0)
			{
				throw new LogicalDeviceCapabilityFeatureNotSupported(string.Format("{0} {1} has no bitmask for {2}", "SetCapabilityFeatureByFirmwareAsync", capabilityFeatureId, logicalDevice));
			}
			ILogicalDevicePidByte capabilitiesUserDisabledPidSim = CapabilitiesUserDisabledPidSim;
			ILogicalDevicePidByte userDisabledFeaturesPid = capabilitiesUserDisabledPidSim ?? new LogicalDevicePidByte(logicalDevice, CapabilitiesUserDisabledPid, LogicalDeviceSessionType.Diagnostic);
			byte existingUserDisabledFeaturesRaw = await userDisabledFeaturesPid.ReadByteAsync(cancellationToken);
			if (!Enumerable.Contains(await GetCapabilitiesSupportedByFirmwareAsync(logicalDevice, cancellationToken), capabilityFeatureId))
			{
				throw new LogicalDeviceCapabilityFeatureNotSupported(string.Format("{0} {1} not supported by firmware {2}", "SetCapabilityFeatureByFirmwareAsync", capabilityFeatureId, logicalDevice));
			}
			byte b = existingUserDisabledFeaturesRaw;
			b = ((!disable) ? ((byte)(b & (byte)(~featureRawBitmask))) : ((byte)(b | featureRawBitmask)));
			TaggedLog.Debug("LogicalDeviceCapabilityWithFeatures", string.Format("{0} Get {1} is 0x{2:X}", "SetCapabilitiesUserDisabledAsync", userDisabledFeaturesPid.PropertyId, existingUserDisabledFeaturesRaw));
			TaggedLog.Debug("LogicalDeviceCapabilityWithFeatures", string.Format("{0} Set {1} to 0x{2:X}", "SetCapabilitiesUserDisabledAsync", userDisabledFeaturesPid.PropertyId, b));
			await userDisabledFeaturesPid.WriteByteAsync(b, cancellationToken);
		}

		public async Task<IEnumerable<LogicalDeviceCapabilityFeatureId>> GetCapabilitiesSupportedByFirmwareAsync(ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures> logicalDevice, CancellationToken cancellationToken)
		{
			ILogicalDevicePidByte capabilitiesSupportedPidSim = CapabilitiesSupportedPidSim;
			return FeatureIdFromRawData(await (capabilitiesSupportedPidSim ?? new LogicalDevicePidByte(logicalDevice, CapabilitiesSupportedByFirmwarePid, LogicalDeviceSessionType.Reprogramming)).ReadByteAsync(cancellationToken));
		}

		public static void DebugDumpCapabilityFeatureIds(IEnumerable<LogicalDeviceCapabilityFeatureId> features)
		{
			bool flag = false;
			foreach (LogicalDeviceCapabilityFeatureId feature in features)
			{
				flag = true;
				TaggedLog.Debug("LogicalDeviceCapabilityWithFeatures", $"    {feature}");
			}
			if (!flag)
			{
				TaggedLog.Debug("LogicalDeviceCapabilityWithFeatures", "    None");
			}
		}

		public async Task<IEnumerable<LogicalDeviceCapabilityFeatureId>> GetCapabilitiesMandatoryByFirmwareAsync(ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures> logicalDevice, CancellationToken cancellationToken)
		{
			ILogicalDevicePidByte capabilitiesMandatoryPidSim = CapabilitiesMandatoryPidSim;
			return FeatureIdFromRawData(await (capabilitiesMandatoryPidSim ?? new LogicalDevicePidByte(logicalDevice, CapabilitiesMandatoryByFirmwarePid, LogicalDeviceSessionType.Reprogramming)).ReadByteAsync(cancellationToken));
		}

		private async Task SetCapabilityFeatureByFirmwareAsync(ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures> logicalDevice, LogicalDeviceCapabilityFeatureId capabilityFeatureId, CancellationToken cancellationToken)
		{
			byte featureBitMask = GetCapabilityFeatureBitMask(capabilityFeatureId);
			if (featureBitMask == 0)
			{
				throw new LogicalDeviceCapabilityFeatureNotSupported(string.Format("{0} {1} has no bitmask for {2}", "SetCapabilityFeatureByFirmwareAsync", capabilityFeatureId, logicalDevice));
			}
			ILogicalDevicePidByte capabilitiesEnabledPidSim = CapabilitiesEnabledPidSim;
			ILogicalDevicePidByte optionalCapabilitiesEnabledPid = capabilitiesEnabledPidSim ?? new LogicalDevicePidByte(logicalDevice, CapabilitiesEnabledPid, LogicalDeviceSessionType.Reprogramming);
			byte featuresEnabledRaw2 = await optionalCapabilitiesEnabledPid.ReadByteAsync(cancellationToken);
			if ((featuresEnabledRaw2 & featureBitMask) != 0)
			{
				return;
			}
			if (!Enumerable.Contains(await GetCapabilitiesSupportedByFirmwareAsync(logicalDevice, cancellationToken), capabilityFeatureId))
			{
				throw new LogicalDeviceCapabilityFeatureNotSupported(string.Format("{0} {1} not supported by firmware {2}", "SetCapabilityFeatureByFirmwareAsync", capabilityFeatureId, logicalDevice));
			}
			featuresEnabledRaw2 = (byte)(featuresEnabledRaw2 | featureBitMask);
			for (int attempt = 0; attempt < 3; attempt++)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}
				await optionalCapabilitiesEnabledPid.WriteByteAsync(featuresEnabledRaw2, cancellationToken);
				await TaskExtension.TryDelay(250, cancellationToken);
				if (IsCapabilityFeatureEnabled(capabilityFeatureId))
				{
					break;
				}
			}
			if (IsCapabilityFeatureEnabled(capabilityFeatureId))
			{
				return;
			}
			throw new LogicalDeviceCapabilityFeatureEnableVerificationFailed(string.Format("{0} {1} attempted to enable, but unable to verify that feature was enabled {2}", "SetCapabilityFeatureByFirmwareAsync", capabilityFeatureId, logicalDevice));
		}

		public async Task<LogicalDeviceCapabilityFeatureStatusPending> SetCapabilityFeatureAsync(ILogicalDeviceWithCapability<ILogicalDeviceCapabilityWithFeatures> logicalDevice, LogicalDeviceCapabilityFeatureId capabilityFeatureId, string requiredSoftwarePartNumber, CancellationToken cancellationToken)
		{
			if (IsCapabilityFeatureEnabled(capabilityFeatureId))
			{
				TaggedLog.Information("LogicalDeviceCapabilityWithFeatures", $"{capabilityFeatureId} Enabled for {logicalDevice}");
				return LogicalDeviceCapabilityFeatureStatusPending.None;
			}
			if (logicalDevice.ActiveConnection != LogicalDeviceActiveConnection.Direct)
			{
				TaggedLog.Information("LogicalDeviceCapabilityWithFeatures", $"{capabilityFeatureId} requires devices to be online before being enabled for {logicalDevice}");
				return LogicalDeviceCapabilityFeatureStatusPending.PendingOnline;
			}
			string text = await logicalDevice.GetSoftwarePartNumberAsync(cancellationToken);
			if (string.Compare(text.Trim(), requiredSoftwarePartNumber.Trim(), StringComparison.OrdinalIgnoreCase) != 0)
			{
				TaggedLog.Information("LogicalDeviceCapabilityWithFeatures", $"{capabilityFeatureId} requires firmware update from `{text}` to `{requiredSoftwarePartNumber}` for {logicalDevice}");
				return LogicalDeviceCapabilityFeatureStatusPending.PendingFirmwareUpdate;
			}
			try
			{
				await SetCapabilityFeatureByFirmwareAsync(logicalDevice, capabilityFeatureId, cancellationToken);
				return LogicalDeviceCapabilityFeatureStatusPending.None;
			}
			catch (LogicalDeviceCapabilityFeatureEnableVerificationFailed logicalDeviceCapabilityFeatureEnableVerificationFailed)
			{
				TaggedLog.Warning("LogicalDeviceCapabilityWithFeatures", $"Trying to enable feature {capabilityFeatureId} but unable to verify for {logicalDevice}: {logicalDeviceCapabilityFeatureEnableVerificationFailed.Message}");
				return LogicalDeviceCapabilityFeatureStatusPending.PendingOther;
			}
			catch (LogicalDeviceCapabilityFeatureNotSupported logicalDeviceCapabilityFeatureNotSupported)
			{
				TaggedLog.Warning("LogicalDeviceCapabilityWithFeatures", $"The feature id {capabilityFeatureId} isn't supported by the firmware for {logicalDevice}: {logicalDeviceCapabilityFeatureNotSupported.Message}");
				throw;
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("LogicalDeviceCapabilityWithFeatures", $"Problem trying to enable {capabilityFeatureId} for {logicalDevice}, will mark as pending: {ex.Message}");
				return LogicalDeviceCapabilityFeatureStatusPending.PendingOther;
			}
		}
	}
}
