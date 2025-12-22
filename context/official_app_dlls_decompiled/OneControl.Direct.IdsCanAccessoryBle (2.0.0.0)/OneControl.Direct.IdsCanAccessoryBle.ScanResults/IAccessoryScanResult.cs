using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Platforms.Shared;
using ids.portable.ble.Platforms.Shared.ScanResults;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions.Contracts;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public interface IAccessoryScanResult : IBleScanResultWithReachability, IBleScanResult
	{
		bool IsInLinkMode { get; }

		bool HasAccessoryId { get; }

		bool HasAccessoryStatus { get; }

		bool HasAccessoryIdsCanExtendedStatus { get; }

		bool HasVersionInfo { get; }

		DateTime AccessoryPidDataLastUpdated { get; }

		DateTime AccessoryStatusDataLastUpdated { get; }

		DateTime AccessoryAbridgedStatusDataLastUpdated { get; }

		DateTime AccessoryDataLastUpdated { get; }

		MAC? AccessoryMacAddress { get; }

		string? SoftwarePartNumber { get; }

		IdsCanAccessoryStatus? GetAccessoryStatus(MAC macAddress);

		byte[]? GetAccessoryAbridgedStatus();

		byte[]? GetAccessoryIdsCanExtendedStatus(MAC macAddress);

		(byte?, byte[]?) GetAccessoryIdsCanExtendedStatusEnhanced(MAC macAddress);

		IEnumerable<AccessoryPidStatus> GetAccessoryPidStatus(MAC macAddress);

		IdsCanAccessoryVersionInfo GetVersionInfo();

		Task<BleDeviceKeySeedExchangeResult> TryLinkVerificationAsync(bool requireLinkMode, CancellationToken cancellationToken, Plugin.BLE.Abstractions.Contracts.IDevice? device = null);
	}
}
