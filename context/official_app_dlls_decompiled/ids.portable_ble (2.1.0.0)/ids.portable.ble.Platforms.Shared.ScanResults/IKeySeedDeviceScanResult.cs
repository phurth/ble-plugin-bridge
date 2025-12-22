using ids.portable.ble.ScanResults;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public interface IKeySeedDeviceScanResult : IBleScanResultWithIdsManufacturingData, IBleScanResult
	{
		uint KeySeedCypher { get; }
	}
}
