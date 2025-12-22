using ids.portable.ble.Platforms.Shared.ScanResults;
using ids.portable.ble.ScanResults;

namespace OneControl.Direct.MyRvLinkBle
{
	public interface IMyRvLinkBleGatewayScanResult : IPairableDeviceScanResult, IBleScanResultWithIdsManufacturingData, IBleScanResult, IKeySeedDeviceScanResult
	{
		RvLinkGatewayType GatewayType { get; }
	}
}
