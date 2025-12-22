using ids.portable.ble.Platforms.Shared.ScanResults;

namespace ids.portable.ble.ScanResults
{
	public interface IBleGatewayScanResult : IPairableDeviceScanResult, IBleScanResultWithIdsManufacturingData, IBleScanResult
	{
		BleGatewayInfo.GatewayVersion AdvertisedGatewayVersion { get; }
	}
}
