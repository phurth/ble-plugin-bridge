using ids.portable.ble.Platforms.Shared.ManufacturingData;

namespace ids.portable.ble.ScanResults
{
	public interface IConnectionCountScanResult : IBleScanResult
	{
		BleConnectionCount? ConnectionCount { get; }
	}
}
