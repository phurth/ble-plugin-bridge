using ids.portable.ble.Platforms.Shared.Reachability;
using ids.portable.ble.ScanResults;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public interface IBleScanResultWithReachability : IBleScanResult
	{
		float? MaxSecondsBetweenConsecutiveAdvertisements { get; }

		BleDeviceReachability Reachability { get; }
	}
}
