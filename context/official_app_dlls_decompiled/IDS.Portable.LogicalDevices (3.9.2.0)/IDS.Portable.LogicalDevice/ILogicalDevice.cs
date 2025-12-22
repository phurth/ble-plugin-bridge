using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevice : IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		ILogicalDeviceId LogicalId { get; }

		string ImmutableUniqueId { get; }

		ILogicalDeviceProduct? Product { get; }

		bool IsRemoteAccessAvailable { get; }

		ILogicalDeviceCircuitId CircuitId { get; }

		ILogicalDeviceService DeviceService { get; }

		IDS_CAN_VERSION_NUMBER CanVersion { get; }

		InTransitLockoutStatus InTransitLockout { get; }

		bool IsLegacyDeviceHazardous { get; }

		bool AllowHazardousOperationAtLockoutLevel1 { get; }

		bool ShouldAutoClearInTransitLockout { get; }

		ILogicalDeviceCapability DeviceCapabilityBasic { get; }

		ILogicalDeviceSessionManager SessionManager { get; }

		bool IsFunctionClassChangeable { get; }

		LogicalDeviceSnapshotMetaDataReadOnly CustomSnapshotData { get; }

		Version ProtocolVersion { get; }

		string? CustomDeviceName { get; }

		string? CustomDeviceNameShort { get; }

		string? CustomDeviceNameShortAbbreviated { get; }

		ConcurrentDictionary<string, object> CustomData { get; }

		event LogicalDeviceChangedEventHandler? DeviceCapabilityChanged;

		void UpdateDeviceOnline(bool online);

		void UpdateDeviceOnline();

		void OnDeviceOnlineChanged();

		void UpdateCircuitId(CIRCUIT_ID circuitId);

		void OnCircuitIdChanged();

		void UpdateNetworkStatus(NETWORK_STATUS networkStatus);

		void OnNetworkStatusChanged(NETWORK_STATUS oldNetworkStatus, NETWORK_STATUS newNetworkStatus);

		void UpdateCanVersion(IDS_CAN_VERSION_NUMBER canVersion);

		void OnCanVersionChanged(IDS_CAN_VERSION_NUMBER oldCanVersion, IDS_CAN_VERSION_NUMBER newCanVersion);

		Task<PidLockoutType> GetInMotionLockoutBehaviorAsync(CancellationToken cancellationToken);

		void UpdateInTransitLockout();

		void OnInTransitLockoutChanged();

		void UpdateDeviceCapability(byte? rawDeviceCapability);

		void OnDeviceCapabilityChanged();

		void UpdateSessionChanged(SESSION_ID sessionId);

		bool Rename(FUNCTION_NAME newFunctionName, int newFunctionInstance);

		Task<bool> TryWaitForRenameAsync(FUNCTION_NAME functionName, int functionInstance, int timeoutMs, CancellationToken cancellationToken);

		IPidDetail GetPidDetail(Pid canPid);

		UInt48? GetCachedPidRawValue(Pid pid);

		void SetCachedPidRawValue(Pid pid, UInt48 value);

		IEnumerable<(Pid Pid, UInt48 Value)> GetCachedPids();

		bool AddDeviceSource(ILogicalDeviceSource deviceSource);

		bool RemoveDeviceSource(ILogicalDeviceSource deviceSource);

		bool IsAssociatedWithDeviceSource(string? deviceSourceToken);

		bool IsAssociatedWithDeviceSource(ILogicalDeviceSource deviceSource);

		bool IsAssociatedWithDeviceSource(IEnumerable<ILogicalDeviceSource> deviceSources);

		bool IsAssociatedWithDeviceSourceToken(string deviceSourceToken);

		Task<string> GetSoftwarePartNumberAsync(CancellationToken cancelToken);

		void SnapshotLoaded(LogicalDeviceSnapshot snapshot);

		void CustomSnapshotDataUpdate(LogicalDeviceSnapshotMetaDataReadOnly snapshotData);

		TLogicalDeviceEx? GetLogicalDeviceEx<TLogicalDeviceEx>() where TLogicalDeviceEx : class, ILogicalDeviceEx;

		void AddLogicalDeviceEx(ILogicalDeviceEx logicalDeviceEx, bool replaceExisting = true);

		void RemoveLogicalDeviceEx(ILogicalDeviceEx logicalDeviceEx);
	}
}
