using ids.portable.ble.ScanResults;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public interface IX180TGatewayScanResult : IBleGatewayScanResult, IPairableDeviceScanResult, IBleScanResultWithIdsManufacturingData, IBleScanResult, IKeySeedDeviceScanResult
	{
	}
}
